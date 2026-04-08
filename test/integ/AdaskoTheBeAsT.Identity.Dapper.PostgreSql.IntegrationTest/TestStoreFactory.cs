using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Npgsql;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.TestCollections;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest;

internal static class TestStoreFactory
{
    public static ApplicationRoleStore CreateRoleStore() =>
        new(CreateConnectionProvider());

    public static ApplicationUserOnlyStore CreateUserOnlyStore() =>
        new(CreateConnectionProvider());

    public static ApplicationUserStore CreateUserStore() =>
        new(CreateConnectionProvider());

    private static TestPostgreSqlConnectionProvider CreateConnectionProvider() =>
        new(DatabaseWithGuidIdFixture.Shared.ConnectionString);

    private sealed class TestPostgreSqlConnectionProvider
        : IIdentityDbConnectionProvider<NpgsqlConnection>
    {
        private readonly string _connectionString;

        public TestPostgreSqlConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public NpgsqlConnection Provide() => new(_connectionString);
    }
}
