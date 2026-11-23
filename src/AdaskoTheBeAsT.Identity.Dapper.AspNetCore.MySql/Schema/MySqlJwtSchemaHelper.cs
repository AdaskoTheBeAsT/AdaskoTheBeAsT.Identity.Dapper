using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySql.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using MySql.Data.MySqlClient;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySql.Schema;

public static class MySqlJwtSchemaHelper
{
    private static readonly SqlDialect Dialect = new(SqlEngine.MySql);

    public static IReadOnlyList<string> BuildSchemaStatements(IdentityJwtMySqlOptions options) =>
        RelationalSchema.Statements(CreateDatabase(options, null, TimeProvider.System, validateConnection: false));

    public static Task EnsureSchemaAndTablesAsync(
        IdentityJwtMySqlOptions options,
        IServiceProvider? services = null,
        CancellationToken cancellationToken = default)
    {
        var database = CreateDatabase(options, services, TimeProvider.System);
        return RelationalSchema.EnsureAsync(database, cancellationToken);
    }

    internal static RelationalDatabase CreateDatabase(
        IdentityJwtMySqlOptions options,
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
