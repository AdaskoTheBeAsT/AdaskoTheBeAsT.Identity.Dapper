using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;

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
        AddClause("where2", sql, parameters, "\r\n  AND ", "WHERE ", string.Empty, isInclusive: false);

    public AdvancedSqlBuilder OrWhere2(
        string sql,
        dynamic? parameters = null) =>
        AddClause("where2", sql, parameters, "\r\n   OR ", "WHERE ", string.Empty, isInclusive: true);

    public AdvancedSqlBuilder Select2(
        string sql,
        dynamic? parameters = null) =>
        AddClause("select2", sql, parameters, " , ", string.Empty, "\r\n", isInclusive: false);

    public AdvancedSqlBuilder InnerJoin2(
        string sql,
        dynamic? parameters = null) =>
        AddClause("innerjoin2", sql, parameters, "\r\nINNER JOIN ", "\r\nINNER JOIN ", "\r\n", isInclusive: false);
}
