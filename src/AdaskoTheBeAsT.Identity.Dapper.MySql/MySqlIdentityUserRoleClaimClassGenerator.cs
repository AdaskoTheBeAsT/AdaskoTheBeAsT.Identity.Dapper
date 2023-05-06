using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlIdentityUserRoleClaimClassGenerator
    : IdentityUserRoleClaimClassGeneratorBase
{
    protected override string ProcessIdentityUserRoleClaimGetRoleClaimsByUserIdSql(IdentityDapperConfiguration config)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Select2("DISTINCT rc.ClaimType AS Type,\r\nrc.ClaimValue AS Value")
            .InnerJoin2($"{config.SchemaPart}`aspnetuserroles` ur ON ur.RoleId=rc.RoleId")
            .Where2($"ur.UserId=@{nameof(IdentityUser.Id)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {config.SchemaPart}`aspnetroleclaims` rc/**innerjoin2**//**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserRoleClaimGetUserAndRoleClaimsByUserIdSql(
        IdentityDapperConfiguration config) =>
        $@"SELECT uc.ClaimType AS Type
      ,uc.ClaimValue AS Value
FROM {config.SchemaPart}`aspnetuserclaims` uc
WHERE uc.UserId=@{nameof(IdentityUser.Id)}
UNION
SELECT rc.ClaimType AS Type
      ,rc.ClaimValue AS Value
FROM {config.SchemaPart}`aspnetroleclaims` rc
INNER JOIN {config.SchemaPart}`aspnetuserroles` ur ON ur.RoleId=rc.RoleId
WHERE ur.UserId=@{nameof(IdentityUser.Id)}";
}
