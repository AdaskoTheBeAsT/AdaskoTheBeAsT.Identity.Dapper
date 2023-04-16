using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleIdentityUserRoleClassGenerator
    : IdentityUserRoleClassGeneratorBase
{
    protected override string ProcessIdentityUserRoleCreateSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", columnNames.Select(s => $"[{s}]")))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                $"INSERT INTO {config.SchemaPart}AspNetUserRoles(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleDeleteSql(IdentityDapperConfiguration config)
    {
        var sqlBuilder = new AdvancedSqlBuilder();

        return sqlBuilder
            .Where2($"{nameof(IdentityUserRole<int>.UserId)}=@{nameof(IdentityUserRole<int>.UserId)}")
            .Where2($"{nameof(IdentityUserRole<int>.RoleId)}=@{nameof(IdentityUserRole<int>.RoleId)}")
            .AddTemplate(
                $"DELETE FROM {config.SchemaPart}AspNetUserRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleGetByUserIdRoleIdSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount);
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"[{columnNames[i]}] AS {propertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityUserRole<int>.UserId)}=@{nameof(IdentityUserRole<int>.UserId)}")
            .Where2($"{nameof(IdentityUserRole<int>.RoleId)}=@{nameof(IdentityUserRole<int>.RoleId)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetUserRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleGetCount(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Where2($"{nameof(IdentityUserRole<int>.UserId)}=@{nameof(IdentityUserRole<int>.UserId)}")
            .Where2($"{nameof(IdentityUserRole<int>.RoleId)}=@{nameof(IdentityUserRole<int>.RoleId)}")
            .AddTemplate(
                $"SELECT COUNT(*)\r\nFROM {config.SchemaPart}AspNetUserRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleGetRoleNamesByUserId(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var template = config.SkipNormalized
            ? $"SELECT r.Name\r\nFROM {config.SchemaPart}AspNetRoles r/**innerjoin2**//**where2**/;"
            : $"SELECT r.NormalizedName\r\nFROM {config.SchemaPart}AspNetRoles r/**innerjoin2**//**where2**/;";
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .InnerJoin2($"{config.SchemaPart}AspNetUserRoles ur ON r.Id=ur.RoleId")
            .Where2($"ur.{nameof(IdentityUserRole<int>.UserId)}=@{nameof(IdentityUserRole<int>.UserId)}")
            .AddTemplate(template)
            .RawSql;
    }
}
