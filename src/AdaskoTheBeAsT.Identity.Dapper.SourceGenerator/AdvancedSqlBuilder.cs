using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public class AdvancedSqlBuilder
    : SqlBuilder
{
    public AdvancedSqlBuilder Insert(
        string sql,
        dynamic? parameters = null) =>
        AddClause("insert", sql, parameters, "\r\n,", string.Empty, string.Empty, isInclusive: false);

    public AdvancedSqlBuilder Values(
        string sql,
        dynamic? parameters = null) =>
        AddClause("values", sql, parameters, "\r\n,", string.Empty, string.Empty, isInclusive: false);
}
