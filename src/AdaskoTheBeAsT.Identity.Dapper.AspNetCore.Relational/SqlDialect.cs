using System.Data.Common;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal sealed class SqlDialect(
    SqlEngine engine,
    Action<DbCommand>? configureCommand = null,
    Action<DbParameter>? configurePayload = null)
{
    public bool IsOracle => engine == SqlEngine.Oracle;

    public bool IsSqlite => engine == SqlEngine.Sqlite;

    public bool IsMySql => engine == SqlEngine.MySql;

    public string PayloadType => engine switch
    {
        SqlEngine.Oracle => "CLOB",
        SqlEngine.MySql => "LONGTEXT",
        _ => "TEXT",
    };

    public string Table(string? schema, string name) =>
        string.IsNullOrEmpty(schema) ? Quote(name) : $"{Quote(schema)}.{Quote(name)}";

    public string Quote(string identifier) =>
        IsMySql ? $"`{identifier}`" : $"\"{identifier}\"";

    public string Column(string name) => Quote(engine switch
    {
        SqlEngine.Oracle => name.ToUpperInvariant(),
        SqlEngine.PostgreSql => name.ToLowerInvariant(),
        _ => name,
    });

    public string Take(string sql) => IsOracle
        ? sql + " FETCH FIRST @p1 ROWS ONLY"
        : sql + " LIMIT @p1";

    public void ConfigureCommand(DbCommand command) => configureCommand?.Invoke(command);

    public void ConfigurePayload(DbParameter parameter) => configurePayload?.Invoke(parameter);
}
