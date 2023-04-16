using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

public class PostgreSqlIdentityUserClassGenerator
    : IdentityUserClassGeneratorBase
{
    private readonly IIdentityHelper _identityHelper;

    public PostgreSqlIdentityUserClassGenerator()
    {
        _identityHelper = new PostgreSqlIdentityHelper();
    }

    protected override string ProcessIdentityUserCreateSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var (localColumnNames, localPropertyNames) = GetListWithoutNormalized(
            config.SkipNormalized,
            columnNames,
            propertyNames);
        var template = _identityHelper.GetInsertTemplate($"{config.SchemaPart}AspNetUsers", config.KeyTypeName);
        return sqlBuilder
            .Insert(string.Join("\r\n,", localColumnNames.Select(s => $"[{s}]")))
            .Values(string.Join("\r\n,", localPropertyNames.Select(s => $"@{s}")))
            .AddTemplate(template)
            .RawSql;
    }

    protected override string ProcessIdentityUserUpdateSql(
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
            .Where2($"{nameof(IdentityUser.Id)}=@{nameof(IdentityUser.Id)}")
            .AddTemplate(
                $"UPDATE {config.SchemaPart}AspNetUsers\r\n/**set2**//**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserDeleteSql(IdentityDapperConfiguration config) =>
        $"DELETE FROM {config.SchemaPart}AspNetUsers WHERE {nameof(IdentityUser.Id)}=@{nameof(IdentityUser.Id)};";

    protected override string ProcessIdentityUserFindByIdSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var (localColumnNames, localPropertyNames) = GetNormalizedSelectList(
            config.SkipNormalized,
            columnNames,
            propertyNames);
        var list = new List<string> { nameof(IdentityUser.Id) };
        for (var i = 0; i < localColumnNames.Count; i++)
        {
            list.Add($"[{localColumnNames[i]}] AS {localPropertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityUser.Id)}=@{nameof(IdentityUser.Id)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetUsers\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserFindByNameSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var (localColumnNames, localPropertyNames) = GetNormalizedSelectList(
            config.SkipNormalized,
            columnNames,
            propertyNames);
        var list = new List<string> { nameof(IdentityUser.Id) };
        for (var i = 0; i < localColumnNames.Count; i++)
        {
            list.Add($"[{localColumnNames[i]}] AS {localPropertyNames[i]}");
        }

        var where = config.SkipNormalized
            ? $"{nameof(IdentityUser.UserName)}=@{nameof(IdentityUser.UserName)}"
            : $"{nameof(IdentityUser.NormalizedUserName)}=@{nameof(IdentityUser.NormalizedUserName)}";

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2(where)
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetUsers\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserFindByEmailSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var (localColumnNames, localPropertyNames) = GetNormalizedSelectList(
            config.SkipNormalized,
            columnNames,
            propertyNames);
        var list = new List<string> { nameof(IdentityUser.Id) };
        for (var i = 0; i < localColumnNames.Count; i++)
        {
            list.Add($"[{localColumnNames[i]}] AS {localPropertyNames[i]}");
        }

        var where = config.SkipNormalized
            ? $"{nameof(IdentityUser.Email)}=@{nameof(IdentityUser.Email)}"
            : $"{nameof(IdentityUser.NormalizedEmail)}=@{nameof(IdentityUser.NormalizedEmail)}";

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2(where)
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetUsers\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserGetUsersForClaimSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var (localColumnNames, localPropertyNames) = GetNormalizedSelectList(
            config.SkipNormalized,
            columnNames,
            propertyNames);
        var list = new List<string> { $"u.{nameof(IdentityUser.Id)}" };
        for (var i = 0; i < localColumnNames.Count; i++)
        {
            list.Add($"u.[{localColumnNames[i]}] AS {localPropertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .InnerJoin2($"{config.SchemaPart}AspNetUserClaims c ON u.Id=c.UserId")
            .Where2("c.ClaimType=@ClaimType")
            .Where2("c.ClaimValue=@ClaimValue")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetUsers u/**innerjoin2**//**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserGetUsersInRoleSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var (localColumnNames, localPropertyNames) = GetNormalizedSelectList(
            config.SkipNormalized,
            columnNames,
            propertyNames);
        var list = new List<string> { $"u.{nameof(IdentityUser.Id)}" };
        for (var i = 0; i < localColumnNames.Count; i++)
        {
            list.Add($"u.[{localColumnNames[i]}] AS {localPropertyNames[i]}");
        }

        var where = config.SkipNormalized
            ? $"r.{nameof(IdentityRole.Name)}=@{nameof(IdentityRole.Name)}"
            : $"r.{nameof(IdentityRole.NormalizedName)}=@{nameof(IdentityRole.NormalizedName)}";

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .InnerJoin2($"{config.SchemaPart}AspNetUserRoles ur ON u.Id=ur.UserId")
            .InnerJoin2($"{config.SchemaPart}AspNetRoles r ON ur.RolesId=r.Id")
            .Where2(where)
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetUsers u/**innerjoin2**//**where2**/;")
            .RawSql;
    }
}
