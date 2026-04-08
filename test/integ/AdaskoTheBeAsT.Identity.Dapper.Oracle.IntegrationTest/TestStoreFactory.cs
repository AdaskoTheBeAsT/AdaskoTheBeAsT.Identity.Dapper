using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Oracle.ManagedDataAccess.Client;
using AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity;
using AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.TestCollections;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest;

internal static class TestStoreFactory
{
    public static ApplicationRoleStore CreateRoleStore() =>
        new(CreateConnectionProvider());

    public static ApplicationUserOnlyStore CreateUserOnlyStore() =>
        new(CreateConnectionProvider());

    public static ApplicationUserStore CreateUserStore() =>
        new(CreateConnectionProvider());

    private static TestOracleConnectionProvider CreateConnectionProvider() =>
        new(DatabaseWithGuidIdFixture.Shared.ConnectionString);

    private sealed class TestOracleConnectionProvider
        : IIdentityDbConnectionProvider<OracleConnection>
    {
        private readonly string _connectionString;

        public TestOracleConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public OracleConnection Provide() => new(_connectionString);
    }
}
