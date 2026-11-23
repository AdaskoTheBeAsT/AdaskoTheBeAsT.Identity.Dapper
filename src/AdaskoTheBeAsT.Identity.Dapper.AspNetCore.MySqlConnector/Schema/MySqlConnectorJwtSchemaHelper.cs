using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySqlConnector.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using global::MySqlConnector;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySqlConnector.Schema;

public static class MySqlConnectorJwtSchemaHelper
{
    private static readonly SqlDialect Dialect = new(SqlEngine.MySql);

    public static IReadOnlyList<string> BuildSchemaStatements(IdentityJwtMySqlConnectorOptions options) =>
        RelationalSchema.Statements(CreateDatabase(options, null, TimeProvider.System, validateConnection: false));

    public static Task EnsureSchemaAndTablesAsync(
        IdentityJwtMySqlConnectorOptions options,
        IServiceProvider? services = null,
        CancellationToken cancellationToken = default)
    {
        var database = CreateDatabase(options, services, TimeProvider.System);
        return RelationalSchema.EnsureAsync(database, cancellationToken);
    }

    internal static RelationalDatabase CreateDatabase(
        IdentityJwtMySqlConnectorOptions options,
        IServiceProvider? services,
        TimeProvider clock,
        bool validateConnection = true)
    {
        ArgumentNullException.ThrowIfNull(options);
        var configuration = options.ToRelational();
        configuration.EnsureValid(validateConnection);
        return new RelationalDatabase(
            () => configuration.ConnectionFactory != null
                ? configuration.ConnectionFactory(services ?? throw new InvalidOperationException("A service provider is required for the configured connection factory."))
                : new MySqlConnection(configuration.ConnectionString),
            configuration,
            Dialect,
            clock);
    }
}
