using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleIdentityRoleClassGenerator
    : IdentityRoleClassGeneratorBase
{
    private readonly IIdentityHelper _identityHelper;

    public OracleIdentityRoleClassGenerator()
    {
        _identityHelper = new OracleIdentityHelper();
    }

    protected override string ProcessIdentityRoleCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnPairs);
        var template = _identityHelper.GetInsertTemplate($"{config.SchemaPart}AspNetRoles", config.KeyTypeName);
        return sqlBuilder
            .Insert(string.Join("\r\n,", localPairs.Select(s => $"[{s.ColumnName}]")))
            .Values(string.Join("\r\n,", localPairs.Select(s => $":{s.PropertyName}")))
            .AddTemplate(template)
            .RawSql;
    }

    protected override string ProcessIdentityRoleUpdateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnPairs);
        var list = new List<string>();
        foreach (var localPair in localPairs)
        {
            list.Add($"[{localPair.ColumnName}]=:{localPair.PropertyName}");
        }

        return sqlBuilder
            .Set2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityRole.Id)}=:{nameof(IdentityRole.Id)}")
            .AddTemplate(
                $"UPDATE {config.SchemaPart}AspNetRoles\r\n/**set2**//**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityRoleDeleteSql(IdentityDapperConfiguration config) =>
        $"DELETE FROM {config.SchemaPart}AspNetRoles WHERE {nameof(IdentityRole.Id)}=:{nameof(IdentityRole.Id)};";

    protected override string ProcessIdentityRoleFindByIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetNormalizedSelectList(
            config.SkipNormalized,
            propertyColumnPairs);
        var list = new List<string> { nameof(IdentityRole.Id) };
        foreach (var localPair in localPairs)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityRole.Id)}=:{nameof(IdentityRole.Id)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityRoleFindByNameSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetNormalizedSelectList(
            config.SkipNormalized,
            propertyColumnPairs);
        var list = new List<string> { nameof(IdentityRole.Id) };
        foreach (var localPair in localPairs)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        var where = config.SkipNormalized
            ? $"{nameof(IdentityRole.Name)}=:{nameof(IdentityRole.NormalizedName)}"
            : $"{nameof(IdentityRole.NormalizedName)}=:{nameof(IdentityRole.NormalizedName)}";

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2(where)
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetRoles\r\n/**where2**/;")
            .RawSql;
    }
}
