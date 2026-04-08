using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using MySql.Data.MySqlClient;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.TestCollections;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest;

internal static class TestStoreFactory
{
    public static ApplicationRoleStore CreateRoleStore() =>
        new(CreateConnectionProvider());

    public static ApplicationUserOnlyStore CreateUserOnlyStore() =>
        new(CreateConnectionProvider());

    public static ApplicationUserStore CreateUserStore() =>
        new(CreateConnectionProvider());

    private static TestMySqlConnectionProvider CreateConnectionProvider() =>
        new(DatabaseWithGuidIdFixture.Shared.ConnectionString);

    private sealed class TestMySqlConnectionProvider
        : IIdentityDbConnectionProvider<MySqlConnection>
    {
        private readonly string _connectionString;

        public TestMySqlConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public MySqlConnection Provide() => new(_connectionString);
    }
}
