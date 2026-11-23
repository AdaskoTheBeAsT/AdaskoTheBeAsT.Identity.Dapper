using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Oracle.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using Oracle.ManagedDataAccess.Client;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Oracle.Schema;

public static class OracleJwtSchemaHelper
{
    private static readonly SqlDialect Dialect = new(
        SqlEngine.Oracle,
        configureCommand: command => ((OracleCommand)command).BindByName = true,
        configurePayload: parameter => ((OracleParameter)parameter).OracleDbType = OracleDbType.Clob);

    public static IReadOnlyList<string> BuildSchemaStatements(IdentityJwtOracleOptions options) =>
        RelationalSchema.Statements(CreateDatabase(options, null, TimeProvider.System, validateConnection: false));

    public static Task EnsureSchemaAndTablesAsync(
        IdentityJwtOracleOptions options,
        IServiceProvider? services = null,
        CancellationToken cancellationToken = default)
    {
        var database = CreateDatabase(options, services, TimeProvider.System);
        return RelationalSchema.EnsureAsync(database, cancellationToken);
    }

    internal static RelationalDatabase CreateDatabase(
        IdentityJwtOracleOptions options,
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
                : new OracleConnection(configuration.ConnectionString),
            configuration,
            Dialect,
            clock);
    }
}
