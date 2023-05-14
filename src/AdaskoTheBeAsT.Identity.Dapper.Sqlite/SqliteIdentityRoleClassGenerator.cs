using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

public class SqliteIdentityRoleClassGenerator
    : IdentityRoleClassGeneratorBase
{
    private readonly IIdentityHelper _identityHelper;

    public SqliteIdentityRoleClassGenerator()
    {
        _identityHelper = new SqliteIdentityHelper();
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
            .Values(string.Join("\r\n,", localPairs.Select(s => $"@{s.PropertyName}")))
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
        foreach (var t in localPairs)
        {
            list.Add($"[{t.ColumnName}]=@{t.PropertyName}");
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
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnPairs);
        var list = new List<string> { nameof(IdentityRole.Id) };
        foreach (var t in localPairs)
        {
            list.Add($"[{t.ColumnName}] AS {t.PropertyName}");
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
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnPairs);
        var list = new List<string> { nameof(IdentityRole.Id) };
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
}
