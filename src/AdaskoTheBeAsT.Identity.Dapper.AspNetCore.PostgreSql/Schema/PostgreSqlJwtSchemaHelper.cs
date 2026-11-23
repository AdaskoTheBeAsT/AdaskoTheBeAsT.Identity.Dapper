using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using Npgsql;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.Schema;

public static class PostgreSqlJwtSchemaHelper
{
    private static readonly SqlDialect Dialect = new(SqlEngine.PostgreSql);

    public static IReadOnlyList<string> BuildSchemaStatements(IdentityJwtPostgreSqlOptions options) =>
        RelationalSchema.Statements(CreateDatabase(options, null, TimeProvider.System, validateConnection: false));

    public static Task EnsureSchemaAndTablesAsync(
        IdentityJwtPostgreSqlOptions options,
        IServiceProvider? services = null,
        CancellationToken cancellationToken = default)
    {
        var database = CreateDatabase(options, services, TimeProvider.System);
        return RelationalSchema.EnsureAsync(database, cancellationToken);
    }

    internal static RelationalDatabase CreateDatabase(
        IdentityJwtPostgreSqlOptions options,
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
                : new NpgsqlConnection(configuration.ConnectionString),
            configuration,
            Dialect,
            clock);
    }
}
