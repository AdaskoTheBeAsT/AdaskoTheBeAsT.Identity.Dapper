using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.TestCollections;
using Microsoft.Data.Sqlite;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest;

internal static class TestStoreFactory
{
    public static ApplicationRoleStore CreateRoleStore() =>
        new(CreateConnectionProvider());

    public static ApplicationUserOnlyStore CreateUserOnlyStore() =>
        new(CreateConnectionProvider());

    public static ApplicationUserStore CreateUserStore() =>
        new(CreateConnectionProvider());

    private static TestSqliteConnectionProvider CreateConnectionProvider() =>
        new(DatabaseWithGuidIdFixture.Shared.ConnectionString);

    private sealed class TestSqliteConnectionProvider
        : IIdentityDbConnectionProvider<SqliteConnection>
    {
        private readonly string _connectionString;

        public TestSqliteConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqliteConnection Provide() => new(_connectionString);
    }
}
