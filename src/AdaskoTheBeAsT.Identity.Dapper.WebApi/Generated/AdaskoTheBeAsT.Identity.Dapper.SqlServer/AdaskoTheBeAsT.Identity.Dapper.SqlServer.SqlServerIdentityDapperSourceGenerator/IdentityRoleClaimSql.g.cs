using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity
{
    public class IdentityRoleClaimSql
        : IIdentityRoleClaimSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO id.AspNetRoleClaims(
[RoleId]
,[ClaimType]
,[ClaimValue])
VALUES(
@RoleId
,@ClaimType
,@ClaimValue);
SELECT SCOPE_IDENTITY();";

        public string DeleteSql { get; } =
            @"DELETE FROM id.AspNetRoleClaims
WHERE RoleId=@RoleId
  AND ClaimType=@ClaimType
  AND ClaimValue=@ClaimValue;";

        public string GetByRoleIdSql { get; } =
            @"SELECT ClaimType AS Type,
ClaimValue AS Value
FROM id.AspNetRoleClaims
WHERE RoleId=@Id;";
    }
}
