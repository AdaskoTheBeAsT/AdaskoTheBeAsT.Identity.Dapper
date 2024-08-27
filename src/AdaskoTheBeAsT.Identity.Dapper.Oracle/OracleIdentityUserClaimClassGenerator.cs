using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleIdentityUserClaimClassGenerator
    : IdentityUserClaimClassGeneratorBase
{
    protected override string ProcessIdentityUserClaimCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var sb = new StringBuilder();
        sb.AppendLine($"DECLARE id {config.SchemaPart}AspNetUserClaims.Id%type;");
        sb.AppendLine($"INSERT INTO {config.SchemaPart}AspNetUserClaims(");
        sb.AppendLine("/**insert**/)");
        sb.AppendLine("VALUES(");
        sb.AppendLine("/**values**/)");
        sb.AppendLine("RETURNING Id INTO id;");
        sb.AppendLine("SELECT id FROM DUAL;");
        return sqlBuilder
            .Insert(string.Join("\r\n,", propertyColumnTypeTriples.Select(s => $"{s.ColumnName}")))
            .Values(string.Join("\r\n,", propertyColumnTypeTriples.Select(s => $":{s.PropertyName}")))
            .AddTemplate(sb.ToString())
            .RawSql;
    }

    protected override string ProcessIdentityUserClaimDeleteSql(IdentityDapperConfiguration config)
    {
        var sqlBuilder = new AdvancedSqlBuilder();

        return sqlBuilder
            .Where2($"{nameof(IdentityUserClaim<int>.UserId)}=:{nameof(IdentityUserClaim<int>.UserId)}")
            .Where2($"{nameof(IdentityUserClaim<int>.ClaimType)}=:{nameof(IdentityUserClaim<int>.ClaimType)}")
            .Where2($"{nameof(IdentityUserClaim<int>.ClaimValue)}=:{nameof(IdentityUserClaim<int>.ClaimValue)}")
            .AddTemplate(
                $"DELETE FROM {config.SchemaPart}AspNetUserClaims\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserClaimGetByUserIdSql(IdentityDapperConfiguration config)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Select2("ClaimType AS Type,\r\nClaimValue AS Value")
            .Where2($"UserId=:{nameof(IdentityUser.Id)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetUserClaims\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserClaimReplaceSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", propertyColumnTypeTriples.Select(s => $"{s.ColumnName}")))
            .Values(string.Join("\r\n,", propertyColumnTypeTriples.Select(s => $":{s.PropertyName}")))
            .AddTemplate(
                $@"IF EXISTS(SELECT Id
            FROM {config.SchemaPart}AspNetUserClaims
            WHERE UserId=:UserId
              AND ClaimType=:ClaimTypeOld
              AND ClaimValue=:ClaimValueOld)
BEGIN
    DELETE FROM {config.SchemaPart}AspNetUserClaims
    WHERE UserId=:UserId
      AND ClaimType=:ClaimTypeOld
      AND ClaimValue=:ClaimValueOld
END;
IF NOT EXISTS(SELECT Id
             FROM {config.SchemaPart}AspNetUserClaims
             WHERE UserId=:UserId
               AND ClaimType=:ClaimTypeNew
               AND ClaimValue=:ClaimValueNew)
BEGIN
    INSERT INTO {config.SchemaPart}AspNetUserClaims(
/**insert**/)
VALUES(
/**values**/);
END;")
            .RawSql;
    }
}
