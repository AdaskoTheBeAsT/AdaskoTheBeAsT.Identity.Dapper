using System.Data;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.TestCollections;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using MySql.Data.MySqlClient;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest;

public sealed class StoreContractTest(DatabaseWithGuidIdFixture fixture)
    : StoreContractTestBase<ApplicationUser, ApplicationRole, ApplicationUserClaim, ApplicationUserLogin,
        ApplicationUserToken, ApplicationRoleClaim, MySqlConnection>, IClassFixture<DatabaseWithGuidIdFixture>
{
    private readonly Provider _provider = new(fixture.ConnectionString);
    protected override ApplicationUserOnlyStore Users() => new(_provider);
    protected override ApplicationRoleStore Roles() => new(_provider);

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(false, "same")]
    [InlineData(true, "same")]
    [InlineData(false, "café ")]
    [InlineData(true, "café ")]
    public async Task IdenticalTokenWritesAreIdempotent(bool useAffectedRows, string? value)
    {
        var settings = new MySqlConnectionStringBuilder(fixture.ConnectionString) { UseAffectedRows = useAffectedRows };
        var provider = new Provider(settings.ConnectionString);
        using var users = new ApplicationUserOnlyStore(provider);
        using var usersWithRoles = new ApplicationUserStore(provider);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString() };
        (await users.CreateAsync(user, CancellationToken.None)).Succeeded.Should().BeTrue();

        foreach (IUserAuthenticationTokenStore<ApplicationUser> store in new IUserAuthenticationTokenStore<ApplicationUser>[] { users, usersWithRoles })
        {
            var name = Guid.NewGuid().ToString();
            await store.SetTokenAsync(user, "idempotent", name, value, CancellationToken.None);
            await store.SetTokenAsync(user, "idempotent", name, value, CancellationToken.None);
            (await store.GetTokenAsync(user, "idempotent", name, CancellationToken.None)).Should().Be(value);
            await store.SetTokenAsync(user, "idempotent", name, "changed", CancellationToken.None);
            (await store.GetTokenAsync(user, "idempotent", name, CancellationToken.None)).Should().Be("changed");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TokenWritesStillRejectConcurrentChanges(bool useAffectedRows)
    {
        var settings = new MySqlConnectionStringBuilder(fixture.ConnectionString) { UseAffectedRows = useAffectedRows };
        var provider = new Provider(settings.ConnectionString);
        using var users = new ApplicationUserOnlyStore(provider);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString() };
        (await users.CreateAsync(user, CancellationToken.None)).Succeeded.Should().BeTrue();
        await users.SetTokenAsync(user, "concurrent", "token", "original", CancellationToken.None);
        using var competing = new TestStore(provider, () =>
            users.SetTokenAsync(user, "concurrent", "token", "intervening", CancellationToken.None));

        await FluentActions.Awaiting(() =>
            competing.SetTokenAsync(user, "concurrent", "token", "attempted", CancellationToken.None)).Should().ThrowExactlyAsync<DBConcurrencyException>();
        (await users.GetTokenAsync(user, "concurrent", "token", CancellationToken.None)).Should().Be("intervening");
    }

    protected override async Task<bool> CompareExchange(ApplicationUserToken token, string? original)
    {
        using var store = new TestStore(_provider);
        using var connection = _provider.Provide();
        return await store.Exchange(connection, token, original);
    }

    protected override async Task<bool> Redeem(ApplicationUser user, string code, Func<Task> barrier)
    {
        using var store = new TestStore(_provider, barrier);
        return await store.RedeemCodeAsync(user, code, CancellationToken.None);
    }

    protected override async Task TokenCommand(ApplicationUser user, string operation, CancellationToken cancellationToken)
    {
        using var store = new TestStore(_provider);
        using var connection = _provider.Provide();
        await store.Command(connection, user, operation, cancellationToken);
    }

    private sealed class Provider(string connectionString) : IIdentityDbConnectionProvider<MySqlConnection>
    {
        public MySqlConnection Provide() => new(connectionString);
    }

    private sealed class TestStore(Provider provider, Func<Task>? barrier = null) : ApplicationUserOnlyStore(provider)
    {
        private bool _waited;
        public Task<bool> Exchange(MySqlConnection connection, ApplicationUserToken token, string? original) =>
            base.TryUpdateTokenImplAsync(connection, token, original, CancellationToken.None);

        public Task Command(MySqlConnection connection, ApplicationUser user, string operation, CancellationToken cancellationToken)
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
            MySqlConnection connection, ApplicationUserToken token, string? originalValue, CancellationToken cancellationToken)
        {
            if (!_waited && barrier != null)
            {
                _waited = true;
                await barrier();
            }

            return await base.TryUpdateTokenImplAsync(connection, token, originalValue, cancellationToken);
        }
    }
}
