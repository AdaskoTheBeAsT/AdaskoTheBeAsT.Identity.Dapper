using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerIdentityRoleClassGenerator
    : IdentityRoleClassGeneratorBase
{
    private readonly IIdentityHelper _identityHelper;

    public SqlServerIdentityRoleClassGenerator()
    {
        _identityHelper = new SqlServerIdentityHelper();
    }

    protected override string ProcessIdentityRoleCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var template = _identityHelper.GetInsertTemplate(
            $"{config.SchemaPart}AspNetRoles",
            config.KeyTypeName,
            config.InsertOwnId);
        return sqlBuilder
            .Insert(string.Join("\r\n,", localPairs.Select(s => $"[{s.ColumnName}]")))
            .Values(string.Join("\r\n,", localPairs.Select(s => $"@{s.PropertyName}")))
            .AddTemplate(template)
            .RawSql;
    }

    protected override string ProcessIdentityRoleUpdateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var list = new List<string>();
        foreach (var localPair in localPairs)
        {
            list.Add($"[{localPair.ColumnName}]=@{localPair.PropertyName}");
        }

        return sqlBuilder
            .Set2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityRole.Id)}=@{nameof(IdentityRole.Id)}")
            .AddTemplate(
                $"UPDATE {config.SchemaPart}AspNetRoles\r\n/**set2**//**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityRoleDeleteSql(IdentityDapperConfiguration config) =>
        $"DELETE FROM {config.SchemaPart}AspNetRoles WHERE {nameof(IdentityRole.Id)}=@{nameof(IdentityRole.Id)};";

    protected override string ProcessIdentityRoleFindByIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetNormalizedSelectList(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var list = new List<string>();
        if (!config.InsertOwnId)
        {
            list.Add(nameof(IdentityRole.Id));
        }

        foreach (var localPair in localPairs)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityRole.Id)}=@{nameof(IdentityRole.Id)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityRoleFindByNameSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetNormalizedSelectList(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var list = new List<string>();
        if (!config.InsertOwnId)
        {
            list.Add(nameof(IdentityRole.Id));
        }

        for (var i = 0; i < localPairs.Count; i++)
        {
            list.Add($"[{localPairs[i].ColumnName}] AS {localPairs[i].PropertyName}");
        }

        var where = config.SkipNormalized
            ? $"{nameof(IdentityRole.Name)}=@{nameof(IdentityRole.NormalizedName)}"
            : $"{nameof(IdentityRole.NormalizedName)}=@{nameof(IdentityRole.NormalizedName)}";

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2(where)
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityRoleGetRolesSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetNormalizedSelectList(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var list = new List<string>();
        if (!config.InsertOwnId)
        {
            list.Add(nameof(IdentityRole.Id));
        }

        for (var i = 0; i < localPairs.Count; i++)
        {
            list.Add($"[{localPairs[i].ColumnName}] AS {localPairs[i].PropertyName}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetRoles;")
            .RawSql;
    }
}
