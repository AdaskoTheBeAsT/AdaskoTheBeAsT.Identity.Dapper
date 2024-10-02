//HintName: IdentityRoleClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityRoleClaimSql
        : IIdentityRoleClaimSql
    {
        public string CreateSql { get; } =
            @"DECLARE id AspNetRoleClaims.Id%type;
INSERT INTO AspNetRoleClaims(
RoleId
,ClaimType
,ClaimValue)
VALUES(
:RoleId
,:ClaimType
,:ClaimValue)
RETURNING Id INTO id;
SELECT id FROM DUAL;
";

        public string DeleteSql { get; } =
            @"DELETE FROM AspNetRoleClaims
WHERE RoleId=:RoleId
  AND ClaimType=:ClaimType
  AND ClaimValue=:ClaimValue;";

        public string GetByRoleIdSql { get; } =
            @"SELECT ClaimType AS Type,
ClaimValue AS Value
FROM AspNetRoleClaims
WHERE RoleId=:Id;";
    }
}
