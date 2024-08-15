using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerIdentityUserClassGenerator
    : IdentityUserClassGeneratorBase
{
    private readonly IIdentityHelper _identityHelper;

    public SqlServerIdentityUserClassGenerator()
    {
        _identityHelper = new SqlServerIdentityHelper();
    }

    protected override string ProcessIdentityUserCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var template = _identityHelper.GetInsertTemplate(
            $"{config.SchemaPart}AspNetUsers",
            config.KeyTypeName,
            config.InsertOwnId);
        return sqlBuilder
            .Insert(string.Join("\r\n,", localPairs.Select(s => $"[{s.ColumnName}]")))
            .Values(string.Join("\r\n,", localPairs.Select(s => $"@{s.PropertyName}")))
            .AddTemplate(template)
            .RawSql;
    }

    protected override string ProcessIdentityUserUpdateSql(
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
            .Where2($"{nameof(IdentityUser.Id)}=@{nameof(IdentityUser.Id)}")
            .AddTemplate(
                $"UPDATE {config.SchemaPart}AspNetUsers\r\n/**set2**//**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserDeleteSql(IdentityDapperConfiguration config) =>
        $"DELETE FROM {config.SchemaPart}AspNetUsers WHERE {nameof(IdentityUser.Id)}=@{nameof(IdentityUser.Id)};";

    protected override string ProcessIdentityUserFindByIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var list = new List<string> { nameof(IdentityUser.Id) };
        foreach (var localPair in localPairs)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
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
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var list = new List<string> { nameof(IdentityUser.Id) };
        foreach (var localPair in localPairs)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        var where = config.SkipNormalized
            ? $"{nameof(IdentityUser.UserName)}=@{nameof(IdentityUser.NormalizedUserName)}"
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
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var list = new List<string> { nameof(IdentityUser.Id) };
        foreach (var localPair in localPairs)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        var where = config.SkipNormalized
            ? $"{nameof(IdentityUser.Email)}=@{nameof(IdentityUser.NormalizedEmail)}"
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
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var list = new List<string> { $"u.{nameof(IdentityUser.Id)}" };
        foreach (var localPair in localPairs)
        {
            list.Add($"u.[{localPair.ColumnName}] AS {localPair.PropertyName}");
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
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var list = new List<string> { $"u.{nameof(IdentityUser.Id)}" };
        foreach (var localPair in localPairs)
        {
            list.Add($"u.[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        var where = config.SkipNormalized
            ? $"r.{nameof(IdentityRole.Name)}=@{nameof(IdentityRole.NormalizedName)}"
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

    protected override string ProcessIdentityUserGetUsersSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var localPairs = GetListWithoutNormalized(
            config.SkipNormalized,
            propertyColumnTypeTriples);
        var list = new List<string> { $"u.{nameof(IdentityUser.Id)}" };
        foreach (var localPair in localPairs)
        {
            list.Add($"u.[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .InnerJoin2($"{config.SchemaPart}AspNetUserRoles ur ON u.Id=ur.UserId")
            .InnerJoin2($"{config.SchemaPart}AspNetRoles r ON ur.RolesId=r.Id")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetUsers u/**innerjoin2**/;")
            .RawSql;
    }
}
