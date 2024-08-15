using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

public class PostgreSqlIdentityRoleClaimClassGenerator
    : IdentityRoleClaimClassGeneratorBase
{
    protected override string ProcessIdentityRoleClaimCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", propertyColumnTypeTriples.Select(s => $"{s.ColumnName.ToLowerInvariant()}")))
            .Values(string.Join("\r\n,", propertyColumnTypeTriples.Select(s => $"@{s.PropertyName}")))
            .AddTemplate(
                $"INSERT INTO {config.SchemaPart}AspNetRoleClaims(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);\r\nSELECT LASTVAL() AS Id;")
            .RawSql;
    }

    protected override string ProcessIdentityRoleClaimDeleteSql(IdentityDapperConfiguration config)
    {
        var sqlBuilder = new AdvancedSqlBuilder();

        return sqlBuilder
            .Where2($"{nameof(IdentityRoleClaim<int>.RoleId)}=@{nameof(IdentityRoleClaim<int>.RoleId)}")
            .Where2($"{nameof(IdentityRoleClaim<int>.ClaimType)}=@{nameof(IdentityRoleClaim<int>.ClaimType)}")
            .Where2($"{nameof(IdentityRoleClaim<int>.ClaimValue)}=@{nameof(IdentityRoleClaim<int>.ClaimValue)}")
            .AddTemplate(
                $"DELETE FROM {config.SchemaPart}AspNetRoleClaims\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityRoleClaimGetByRoleIdSql(IdentityDapperConfiguration config)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Select2("ClaimType AS \"\"Type\"\",\r\nClaimValue AS \"\"Value\"\"")
            .Where2($"RoleId=@{nameof(IdentityRole.Id)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}AspNetRoleClaims\r\n/**where2**/;")
            .RawSql;
    }
}
