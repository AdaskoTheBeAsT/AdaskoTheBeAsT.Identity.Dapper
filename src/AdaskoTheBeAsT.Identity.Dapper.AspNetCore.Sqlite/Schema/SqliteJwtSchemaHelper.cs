using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.Options;
using Microsoft.Data.Sqlite;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.Schema;

public static class SqliteJwtSchemaHelper
{
    private static readonly SqlDialect Dialect = new(SqlEngine.Sqlite);

    public static IReadOnlyList<string> BuildSchemaStatements(IdentityJwtSqliteOptions options) =>
        RelationalSchema.Statements(CreateDatabase(options, null, TimeProvider.System, validateConnection: false));

    public static Task EnsureSchemaAndTablesAsync(
        IdentityJwtSqliteOptions options,
        IServiceProvider? services = null,
        CancellationToken cancellationToken = default)
    {
        var database = CreateDatabase(options, services, TimeProvider.System);
        return RelationalSchema.EnsureAsync(database, cancellationToken);
    }

    internal static RelationalDatabase CreateDatabase(
        IdentityJwtSqliteOptions options,
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
                : new SqliteConnection(configuration.ConnectionString),
            configuration,
            Dialect,
            clock);
    }
}
