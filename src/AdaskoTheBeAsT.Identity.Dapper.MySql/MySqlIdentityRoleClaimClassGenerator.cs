using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlIdentityRoleClaimClassGenerator
    : IdentityRoleClaimClassGeneratorBase
{
    protected override string ProcessIdentityRoleClaimCreateSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", columnNames.Select(s => $"`{s}`")))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                "INSERT INTO `aspnetroleclaims`(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);\r\nSELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER);")
            .RawSql;
    }

    protected override string ProcessIdentityRoleClaimDeleteSql()
    {
        var sqlBuilder = new AdvancedSqlBuilder();

        return sqlBuilder
            .Where2($"{nameof(IdentityRoleClaim<int>.RoleId)}=@{nameof(IdentityRoleClaim<int>.RoleId)}")
            .Where2($"{nameof(IdentityRoleClaim<int>.ClaimType)}=@{nameof(IdentityRoleClaim<int>.ClaimType)}")
            .Where2($"{nameof(IdentityRoleClaim<int>.ClaimValue)}=@{nameof(IdentityRoleClaim<int>.ClaimValue)}")
            .AddTemplate(
                "DELETE FROM `aspnetroleclaims`\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityRoleClaimGetByRoleIdSql()
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Select2("ClaimType AS Type,\r\nClaimValue AS Value")
            .Where2($"RoleId=@{nameof(IdentityRole.Id)}")
            .AddTemplate(
                "SELECT /**select2**/FROM `aspnetroleclaims`\r\n/**where2**/;")
            .RawSql;
    }
}
