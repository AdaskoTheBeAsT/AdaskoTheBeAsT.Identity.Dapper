using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.SqlClient;

public class AdvancedSqlBuilder
    : SqlBuilder
{
    public AdvancedSqlBuilder Insert(
        string sql,
        dynamic? parameters = null) =>
        AddClause("insert", sql, parameters, "\n,", string.Empty, string.Empty, false);

    public AdvancedSqlBuilder Values(
        string sql,
        dynamic? parameters = null) =>
        AddClause("values", sql, parameters, "\n,", string.Empty, string.Empty, false);
}
