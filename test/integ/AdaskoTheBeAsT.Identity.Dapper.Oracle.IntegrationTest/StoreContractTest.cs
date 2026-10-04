using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity;
using AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.TestCollections;
using AwesomeAssertions;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest;

public sealed class StoreContractTest(DatabaseWithGuidIdFixture fixture)
    : StoreContractTestBase<ApplicationUser, ApplicationRole, ApplicationUserClaim, ApplicationUserLogin,
        ApplicationUserToken, ApplicationRoleClaim, OracleConnection>, IClassFixture<DatabaseWithGuidIdFixture>
{
    private readonly Provider _provider = new(fixture.ConnectionString);

    [Fact]
    public async Task InheritedDateAndNullableDefaultsRoundTrip()
    {
        using var store = Users();
        var date = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), CreatedAt = date, DisplayLabel = null };
        var created = await store.CreateAsync(user, CancellationToken.None);
        created.Succeeded.Should().BeTrue("{0}", string.Join("; ", created.Errors.Select(e => e.Description)));
        var loaded = (await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None))!;
        loaded.CreatedAt.Should().Be(date);
        loaded.DisplayLabel.Should().BeNull();
        loaded.CreatedAt = null;
        (await store.UpdateAsync(loaded, CancellationToken.None)).Succeeded.Should().BeTrue();
        (await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None))!.CreatedAt.Should().BeNull();
    }

    [Fact]
    public async Task AuxiliaryFactoriesPersistCustomFieldsAndReplacementKeepsClaimIdentity()
    {
        using var store = new ApplicationUserStore(_provider);
        using var roles = Roles();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString() };
        var role = new ApplicationRole { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };
        (await store.CreateAsync(user, CancellationToken.None)).Succeeded.Should().BeTrue();
        (await roles.CreateAsync(role, CancellationToken.None)).Succeeded.Should().BeTrue();
        await store.AddClaimsAsync(user, new[] { new Claim("old", "value") }, CancellationToken.None);
        await roles.AddClaimAsync(role, new Claim(nameof(role), "value"), CancellationToken.None);
        await store.AddLoginAsync(user, new Microsoft.AspNetCore.Identity.UserLoginInfo("provider", user.Id.ToString(), "name"), CancellationToken.None);
        await store.AddToRoleAsync(user, role.Name, CancellationToken.None);
        await store.SetTokenAsync(user, "provider", "name", "value", CancellationToken.None);
        using var connection = _provider.Provide();
        var parameters = new { Id = user.Id };
        var before = await connection.QuerySingleAsync<ApplicationUserClaim>("SELECT Id, AuditSource FROM ASPNETUSERCLAIMS WHERE UserId=:Id", parameters);
        before.AuditSource.Should().Be("user claim");
        await store.ReplaceClaimAsync(user, new Claim("old", "value"), new Claim("new", "value"), CancellationToken.None);
        var after = await connection.QuerySingleAsync<ApplicationUserClaim>("SELECT Id, AuditSource, ClaimType FROM ASPNETUSERCLAIMS WHERE UserId=:Id", parameters);
        after.Id.Should().Be(before.Id);
        after.AuditSource.Should().Be(before.AuditSource);
        after.ClaimType.Should().Be("new");
        (await connection.QuerySingleAsync<string>("SELECT AuditSource FROM ASPNETROLECLAIMS WHERE RoleId=:Id", new { Id = role.Id })).Should().Be("role claim");
        (await connection.QuerySingleAsync<string>("SELECT AuditSource FROM ASPNETUSERLOGINS WHERE UserId=:Id", parameters)).Should().Be("login");
        (await connection.QuerySingleAsync<string>("SELECT AuditSource FROM ASPNETUSERROLES WHERE UserId=:Id", parameters)).Should().Be("role link");
        (await connection.QuerySingleAsync<string>("SELECT AuditSource FROM ASPNETUSERTOKENS WHERE UserId=:Id", parameters)).Should().Be("token");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScopedLoginLookupMatchesUserProviderAndKey(bool includeRoles)
    {
        using var users = Users();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString() };
        (await users.CreateAsync(user, CancellationToken.None)).Succeeded.Should().BeTrue();
        var providerKey = Guid.NewGuid().ToString();
        await users.AddLoginAsync(user, new Microsoft.AspNetCore.Identity.UserLoginInfo("scoped", providerKey, "name"), CancellationToken.None);
        using var connection = _provider.Provide();
        using var userOnly = new TestStore(_provider);
        using var withRoles = new LoginTestStore(_provider);
        Func<OracleConnection, Guid, string, string, Task<ApplicationUserLogin?>> lookup = includeRoles
            ? withRoles.FindLoginAsync
            : userOnly.FindLoginAsync;
        (await lookup(connection, user.Id, "scoped", providerKey))!.UserId.Should().Be(user.Id);
        (await lookup(connection, Guid.NewGuid(), "scoped", providerKey)).Should().BeNull();
        (await lookup(connection, user.Id, "other", providerKey)).Should().BeNull();
        (await lookup(connection, user.Id, "scoped", "other")).Should().BeNull();
    }

    protected override ApplicationUserOnlyStore Users() => new(_provider);

    protected override ApplicationRoleStore Roles() => new(_provider);

    protected override async Task<bool> CompareExchangeAsync(ApplicationUserToken token, string? original)
    {
        using var store = new TestStore(_provider);
        using var connection = _provider.Provide();
        return await store.ExchangeAsync(connection, token, original);
    }

    protected override async Task<bool> RedeemAsync(ApplicationUser user, string code, Func<Task> barrier)
    {
        using var store = new TestStore(_provider, barrier);
        return await store.RedeemCodeAsync(user, code, CancellationToken.None);
    }

    protected override async Task TokenCommandAsync(ApplicationUser user, string operation, CancellationToken cancellationToken)
    {
        using var store = new TestStore(_provider);
        using var connection = _provider.Provide();
        await store.CommandAsync(connection, user, operation, cancellationToken);
    }

    private sealed class Provider(string connectionString) : IIdentityDbConnectionProvider<OracleConnection>
    {
        public OracleConnection Provide() => new(connectionString);
    }

    private sealed class TestStore(Provider provider, Func<Task>? barrier = null) : ApplicationUserOnlyStore(provider)
    {
        private bool _waited;

        public Task<ApplicationUserLogin?> FindLoginAsync(OracleConnection connection, Guid userId, string provider, string key) =>
                            FindUserLoginImplAsync(connection, userId, provider, key, CancellationToken.None);

        public Task<bool> ExchangeAsync(OracleConnection connection, ApplicationUserToken token, string? original) =>
                            base.TryUpdateTokenImplAsync(connection, token, original, CancellationToken.None);

        public Task CommandAsync(OracleConnection connection, ApplicationUser user, string operation, CancellationToken cancellationToken)
        {
            var token = new ApplicationUserToken { UserId = user.Id, LoginProvider = "contract", Name = "cancel" };
            return operation switch
            {
                "find" => FindTokenImplAsync(connection, user, token.LoginProvider, token.Name, cancellationToken),
                "add" => AddUserTokenImplAsync(connection, token, cancellationToken),
                "remove" => RemoveUserTokenImplAsync(connection, token, cancellationToken),
                _ => TryUpdateTokenImplAsync(connection, token, null, cancellationToken),
            };
        }

        protected override async Task<bool> TryUpdateTokenImplAsync(
                            OracleConnection connection, ApplicationUserToken token, string? originalValue, CancellationToken cancellationToken)
        {
            if (!_waited && barrier != null)
            {
                _waited = true;
                await (barrier?.Invoke() ?? throw new ArgumentNullException(nameof(barrier)));
            }

            return await base.TryUpdateTokenImplAsync(connection, token, originalValue, cancellationToken);
        }
    }

    private sealed class LoginTestStore(Provider provider) : ApplicationUserStore(provider)
    {
        public Task<ApplicationUserLogin?> FindLoginAsync(OracleConnection connection, Guid userId, string loginProvider, string key) =>
                            FindUserLoginImplAsync(connection, userId, loginProvider, key, CancellationToken.None);
    }
}
