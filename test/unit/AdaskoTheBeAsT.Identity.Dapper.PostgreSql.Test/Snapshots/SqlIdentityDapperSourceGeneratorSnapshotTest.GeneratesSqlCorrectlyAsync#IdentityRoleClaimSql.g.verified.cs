//HintName: IdentityRoleClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityRoleClaimSql
        : IIdentityRoleClaimSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO AspNetRoleClaims(
roleid
,claimtype
,claimvalue)
VALUES(
@RoleId
,@ClaimType
,@ClaimValue);
SELECT LASTVAL() AS Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM AspNetRoleClaims
WHERE RoleId=@RoleId
  AND ClaimType=@ClaimType
  AND ClaimValue=@ClaimValue;";

        public string GetByRoleIdSql { get; } =
            @"SELECT ClaimType AS ""Type"",
ClaimValue AS ""Value""
FROM AspNetRoleClaims
WHERE RoleId=@Id;";
    }
}
