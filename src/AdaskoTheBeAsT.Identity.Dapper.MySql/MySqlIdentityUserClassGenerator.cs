using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlIdentityUserClassGenerator
    : IdentityUserClassGeneratorBase
{
    protected override string ProcessIdentityUserCreateSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", columnNames.Select(s => $"`{s}`")))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                "INSERT INTO `aspnetusers`(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);\r\nSELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER);")
            .RawSql;
    }

    protected override string ProcessIdentityUserUpdateSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount);
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"`{columnNames[i]}`=@{propertyNames[i]}");
        }

        return sqlBuilder
            .Set2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityUser.Id)}=@{nameof(IdentityUser.Id)}")
            .AddTemplate(
                "UPDATE `aspnetusers`\r\n/**set2**//**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserDeleteSql() =>
        $"DELETE FROM `aspnetusers` WHERE {nameof(IdentityUser.Id)}=@{nameof(IdentityUser.Id)};";

    protected override string ProcessIdentityUserFindByIdSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount) { nameof(IdentityUser.Id) };
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"`{columnNames[i]}` AS {propertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityUser.Id)}=@{nameof(IdentityUser.Id)}")
            .AddTemplate(
                "SELECT /**select2**/FROM `aspnetusers`\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserFindByNameSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount) { nameof(IdentityUser.Id) };
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"`{columnNames[i]}` AS {propertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityUser.NormalizedUserName)}=@{nameof(IdentityUser.NormalizedUserName)}")
            .AddTemplate(
                "SELECT /**select2**/FROM `aspnetusers`\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserFindByEmailSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount) { nameof(IdentityUser.Id) };
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"`{columnNames[i]}` AS {propertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityUser.NormalizedEmail)}=@{nameof(IdentityUser.NormalizedEmail)}")
            .AddTemplate(
                "SELECT /**select2**/FROM `aspnetusers`\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserGetUsersForClaimSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount) { $"u.{nameof(IdentityUser.Id)}" };
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"u.[{columnNames[i]}] AS {propertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .InnerJoin2("`aspnetuserclaims` c ON u.Id=c.UserId")
            .Where2("c.ClaimType=@ClaimType")
            .Where2("c.ClaimValue=@ClaimValue")
            .AddTemplate(
                "SELECT /**select2**/FROM `aspnetusers` u/**innerjoin2**//**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserGetUsersInRoleSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount) { $"u.{nameof(IdentityUser.Id)}" };
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"u.[{columnNames[i]}] AS {propertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .InnerJoin2("`aspnetuserroles` ur ON u.Id=ur.UserId")
            .InnerJoin2("`aspnetroles` r ON ur.RolesId=r.Id")
            .Where2($"r.{nameof(IdentityRole.NormalizedName)}=@{nameof(IdentityRole.NormalizedName)}")
            .AddTemplate(
                "SELECT /**select2**/FROM `aspnetusers` u/**innerjoin2**//**where2**/;")
            .RawSql;
    }
}
