using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace Sample.SqlServer
{
    public class IdentityRoleClaimSql
        : IIdentityRoleClaimSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetRoleClaims(
[RoleId]
,[ClaimType]
,[ClaimValue])
VALUES(
@RoleId
,@ClaimType
,@ClaimValue);
SELECT SCOPE_IDENTITY();";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetRoleClaims
WHERE RoleId=@RoleId
  AND ClaimType=@ClaimType
  AND ClaimValue=@ClaimValue;";

        public string GetByRoleIdSql { get; } =
            @"SELECT ClaimType AS Type,
ClaimValue AS Value
FROM dbo.AspNetRoleClaims
WHERE RoleId=@Id;";
    }
}
