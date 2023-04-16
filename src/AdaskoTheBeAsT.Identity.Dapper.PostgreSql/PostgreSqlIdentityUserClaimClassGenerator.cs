using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

public class PostgreSqlIdentityUserClaimClassGenerator
    : IdentityUserClaimClassGeneratorBase
{
    protected override string ProcessIdentityUserClaimCreateSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", columnNames.Select(s => $"[{s}]")))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                $"INSERT INTO {config.SchemaPart}AspNetUserClaims(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);\r\nSELECT LASTVAL() AS Id;")
            .RawSql;
    }

    protected override string ProcessIdentityUserClaimDeleteSql(IdentityDapperConfiguration config)
    {
        var sqlBuilder = new AdvancedSqlBuilder();

        return sqlBuilder
            .Where2($"{nameof(IdentityUserClaim<int>.UserId)}=@{nameof(IdentityUserClaim<int>.UserId)}")
            .Where2($"{nameof(IdentityUserClaim<int>.ClaimType)}=@{nameof(IdentityUserClaim<int>.ClaimType)}")
            .Where2($"{nameof(IdentityUserClaim<int>.ClaimValue)}=@{nameof(IdentityUserClaim<int>.ClaimValue)}")
            .AddTemplate(
                $"DELETE FROM {config.SchemaPart}AspNetUserClaims\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserClaimGetByUserIdSql(IdentityDapperConfiguration config)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Select2("ClaimType AS Type,\r\nClaimValue AS Value")
            .Where2($"UserId=@{nameof(IdentityUser.Id)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetUserClaims\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserClaimReplaceSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", columnNames.Select(s => $"[{s}]")))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                $@"IF EXISTS(SELECT Id
            FROM {config.SchemaPart}AspNetUserClaims
            WHERE UserId=@UserId
              AND ClaimType=@ClaimTypeOld
              AND ClaimValue=@ClaimValueOld)
BEGIN
    DELETE FROM {config.SchemaPart}AspNetUserClaims
    WHERE UserId=@UserId
      AND ClaimType=@ClaimTypeOld
      AND ClaimValue=@ClaimValueOld
END;
IF NOT EXISTS(SELECT Id
             FROM {config.SchemaPart}AspNetUserClaims
             WHERE UserId=@UserId
               AND ClaimType=@ClaimTypeNew
               AND ClaimValue=@ClaimValueNew)
BEGIN
    INSERT INTO {config.SchemaPart}AspNetUserClaims(
/**insert**/)
VALUES(
/**values**/);
END;")
            .RawSql;
    }
}
