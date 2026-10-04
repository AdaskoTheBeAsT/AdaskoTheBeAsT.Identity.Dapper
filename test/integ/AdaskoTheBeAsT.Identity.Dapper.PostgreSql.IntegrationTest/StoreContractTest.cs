using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.TestCollections;
using Npgsql;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest;

public sealed class StoreContractTest(DatabaseWithGuidIdFixture fixture)
    : StoreContractTestBase<ApplicationUser, ApplicationRole, ApplicationUserClaim, ApplicationUserLogin,
        ApplicationUserToken, ApplicationRoleClaim, NpgsqlConnection>, IClassFixture<DatabaseWithGuidIdFixture>
{
    private readonly Provider _provider = new(fixture.ConnectionString);

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

    private sealed class Provider(string connectionString) : IIdentityDbConnectionProvider<NpgsqlConnection>
    {
        public NpgsqlConnection Provide() => new(connectionString);
    }

    private sealed class TestStore(Provider provider, Func<Task>? barrier = null) : ApplicationUserOnlyStore(provider)
    {
        private bool _waited;

        public Task<bool> ExchangeAsync(NpgsqlConnection connection, ApplicationUserToken token, string? original) =>
                    base.TryUpdateTokenImplAsync(connection, token, original, CancellationToken.None);

        public Task CommandAsync(NpgsqlConnection connection, ApplicationUser user, string operation, CancellationToken cancellationToken)
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
                    NpgsqlConnection connection, ApplicationUserToken token, string? originalValue, CancellationToken cancellationToken)
        {
            if (!_waited && barrier != null)
            {
                _waited = true;
                await (barrier?.Invoke() ?? throw new ArgumentNullException(nameof(barrier)));
            }

            return await base.TryUpdateTokenImplAsync(connection, token, originalValue, cancellationToken);
        }
    }
}
