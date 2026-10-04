using System;
using System.Globalization;
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

    public AdvancedSqlBuilder Skip(
        int skip,
        DbType dbType) =>
        (AddClause(
            nameof(skip),
            GetSkipClause(skip, dbType),
            parameters: null,
            string.Empty,
            string.Empty,
            string.Empty,
            isInclusive: false) as AdvancedSqlBuilder)!;

    public AdvancedSqlBuilder Take(
        int take,
        DbType dbType) =>
        (AddClause(
            nameof(take),
            GetTakeClause(take, dbType),
            parameters: null,
            string.Empty,
            string.Empty,
            string.Empty,
            isInclusive: false) as AdvancedSqlBuilder)!;

    private static string GetSkipClause(int skip, DbType dbType)
    {
        var value = skip.ToString(CultureInfo.InvariantCulture);
        return dbType switch
        {
            DbType.SqlServer => $"OFFSET {value} ROWS",
            DbType.Postgres => $"OFFSET {value}",
            DbType.MySql => $"OFFSET {value}",
            DbType.Sqlite => $"OFFSET {value}",
            DbType.Oracle => $"OFFSET {value} ROWS",
            _ => throw new NotSupportedException($"Unsupported database type: {dbType}"),
        };
    }

    private static string GetTakeClause(int take, DbType dbType)
    {
        var value = take.ToString(CultureInfo.InvariantCulture);
        return dbType switch
        {
            DbType.SqlServer => $"FETCH NEXT {value} ROWS ONLY",
            DbType.Postgres => $"LIMIT {value}",
            DbType.MySql => $"LIMIT {value}",
            DbType.Sqlite => $"LIMIT {value}",
            DbType.Oracle => $"FETCH NEXT {value} ROWS ONLY",
            _ => throw new NotSupportedException($"Unsupported database type: {dbType}"),
        };
    }
}
