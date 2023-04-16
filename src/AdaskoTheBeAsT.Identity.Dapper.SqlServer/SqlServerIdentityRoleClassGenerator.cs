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
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var (localColumnNames, localPropertyNames) = GetListWithoutNormalized(
            config.SkipNormalized,
            columnNames,
            propertyNames);
        var template = _identityHelper.GetInsertTemplate($"{config.SchemaPart}AspNetRoles", config.KeyTypeName);
        return sqlBuilder
            .Insert(string.Join("\r\n,", localColumnNames.Select(s => $"[{s}]")))
            .Values(string.Join("\r\n,", localPropertyNames.Select(s => $"@{s}")))
            .AddTemplate(template)
            .RawSql;
    }

    protected override string ProcessIdentityRoleUpdateSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var (localColumnNames, localPropertyNames) = GetListWithoutNormalized(
            config.SkipNormalized,
            columnNames,
            propertyNames);
        var list = new List<string>();
        for (var i = 0; i < localColumnNames.Count; i++)
        {
            list.Add($"[{localColumnNames[i]}]=@{localPropertyNames[i]}");
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
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var (localColumnNames, localPropertyNames) = GetNormalizedSelectList(
            config.SkipNormalized,
            columnNames,
            propertyNames);
        var list = new List<string> { nameof(IdentityRole.Id) };
        for (var i = 0; i < localColumnNames.Count; i++)
        {
            list.Add($"[{localColumnNames[i]}] AS {localPropertyNames[i]}");
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
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var (localColumnNames, localPropertyNames) = GetNormalizedSelectList(
            config.SkipNormalized,
            columnNames,
            propertyNames);
        var list = new List<string> { nameof(IdentityRole.Id) };
        for (var i = 0; i < localColumnNames.Count; i++)
        {
            list.Add($"[{localColumnNames[i]}] AS {localPropertyNames[i]}");
        }

        var where = config.SkipNormalized
            ? $"{nameof(IdentityRole.Name)}=@{nameof(IdentityRole.Name)}"
            : $"{nameof(IdentityRole.NormalizedName)}=@{nameof(IdentityRole.NormalizedName)}";

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2(where)
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetRoles\r\n/**where2**/;")
            .RawSql;
    }
}
