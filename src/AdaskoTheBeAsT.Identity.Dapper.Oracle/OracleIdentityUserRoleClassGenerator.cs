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
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", propertyColumnPairs.Select(s => $"[{s.ColumnName}]")))
            .Values(string.Join("\r\n,", propertyColumnPairs.Select(s => $":{s.PropertyName}")))
            .AddTemplate(
                $"INSERT INTO {config.SchemaPart}AspNetUserRoles(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleDeleteSql(IdentityDapperConfiguration config)
    {
        var sqlBuilder = new AdvancedSqlBuilder();

        return sqlBuilder
            .Where2($"{nameof(IdentityUserRole<int>.UserId)}=:{nameof(IdentityUserRole<int>.UserId)}")
            .Where2($"{nameof(IdentityUserRole<int>.RoleId)}=:{nameof(IdentityUserRole<int>.RoleId)}")
            .AddTemplate(
                $"DELETE FROM {config.SchemaPart}AspNetUserRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleGetByUserIdRoleIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var list = new List<string>(propertyColumnPairs.Count);
        foreach (var localPair in propertyColumnPairs)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityUserRole<int>.UserId)}=:{nameof(IdentityUserRole<int>.UserId)}")
            .Where2($"{nameof(IdentityUserRole<int>.RoleId)}=:{nameof(IdentityUserRole<int>.RoleId)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetUserRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleGetCount(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Where2($"{nameof(IdentityUserRole<int>.UserId)}=:{nameof(IdentityUserRole<int>.UserId)}")
            .Where2($"{nameof(IdentityUserRole<int>.RoleId)}=:{nameof(IdentityUserRole<int>.RoleId)}")
            .AddTemplate(
                $"SELECT COUNT(*)\r\nFROM {config.SchemaPart}AspNetUserRoles\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleGetRoleNamesByUserId(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var template = config.SkipNormalized
            ? $"SELECT r.Name\r\nFROM {config.SchemaPart}AspNetRoles r/**innerjoin2**//**where2**/;"
            : $"SELECT r.NormalizedName\r\nFROM {config.SchemaPart}AspNetRoles r/**innerjoin2**//**where2**/;";
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .InnerJoin2($"{config.SchemaPart}AspNetUserRoles ur ON r.Id=ur.RoleId")
            .Where2($"ur.{nameof(IdentityUserRole<int>.UserId)}=:{nameof(IdentityUserRole<int>.UserId)}")
            .AddTemplate(template)
            .RawSql;
    }
}
