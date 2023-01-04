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

    public AdvancedSqlBuilder Set2(
        string sql,
        dynamic? parameters = null) =>
        AddClause("set2", sql, parameters, " , ", "SET ", "\r\n", isInclusive: false);

    public AdvancedSqlBuilder Where2(
        string sql,
        dynamic? parameters = null) =>
        AddClause("where2", sql, parameters, " AND ", "WHERE ", "\r\n", false);

    public AdvancedSqlBuilder OrWhere2(
        string sql,
        dynamic? parameters = null) =>
        AddClause("where2", sql, parameters, " OR ", "WHERE ", "\r\n", true);
}
