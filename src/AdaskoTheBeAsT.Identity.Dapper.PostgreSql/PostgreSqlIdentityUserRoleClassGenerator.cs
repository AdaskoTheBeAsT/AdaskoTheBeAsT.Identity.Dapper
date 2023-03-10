using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

public class PostgreSqlIdentityUserRoleClassGenerator
    : IdentityUserRoleClassGeneratorBase
{
    protected override string ProcessIdentityUserRoleCreateSql(
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", columnNames.Select(s => $"[{s}]")))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                $"INSERT INTO {schemaPart}AspNetUserRoles(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleDeleteSql(string schemaPart)
    {
        var sqlBuilder = new AdvancedSqlBuilder();

        return sqlBuilder
            .Where2($"{nameof(IdentityUserRole<int>.UserId)}=@{nameof(IdentityUserRole<int>.UserId)}")
            .Where2($"{nameof(IdentityUserRole<int>.RoleId)}=@{nameof(IdentityUserRole<int>.RoleId)}")
            .AddTemplate(
                $"DELETE FROM {schemaPart}AspNetUserRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleGetByUserIdRoleIdSql(
        string schemaPart,
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
                $"SELECT /**select2**/FROM {schemaPart}AspNetUserRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleGetCount(
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Where2($"{nameof(IdentityUserRole<int>.UserId)}=@{nameof(IdentityUserRole<int>.UserId)}")
            .Where2($"{nameof(IdentityUserRole<int>.RoleId)}=@{nameof(IdentityUserRole<int>.RoleId)}")
            .AddTemplate(
                $"SELECT COUNT(*)\r\nFROM {schemaPart}AspNetUserRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleGetRoleNamesByUserId(
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .InnerJoin2($"{schemaPart}AspNetUserRoles ur ON r.Id=ur.RoleId")
            .Where2($"ur.{nameof(IdentityUserRole<int>.UserId)}=@{nameof(IdentityUserRole<int>.UserId)}")
            .AddTemplate(
                $"SELECT r.NormalizedName\r\nFROM {schemaPart}AspNetRoles r/**innerjoin2**//**where2**/;")
            .RawSql;
    }
}
