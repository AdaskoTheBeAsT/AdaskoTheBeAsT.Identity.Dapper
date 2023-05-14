using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity
{
    public class IdentityUserRoleClaimSql
        : IIdentityUserRoleClaimSql
    {
        public string GetRoleClaimsByUserIdSql { get; } =
            @"SELECT DISTINCT rc.ClaimType AS Type,
rc.ClaimValue AS Value
FROM dbo.AspNetRoleClaims rc
INNER JOIN dbo.AspNetUserRoles ur ON ur.RoleId=rc.RoleId
WHERE ur.UserId=@Id;";

        public string GetUserAndRoleClaimsByUserIdSql { get; } =
            @"SELECT uc.ClaimType AS Type
      ,uc.ClaimValue AS Value
FROM dbo.AspNetUserClaims uc
WHERE uc.UserId=@Id
UNION
SELECT rc.ClaimType AS Type
      ,rc.ClaimValue AS Value
FROM dbo.AspNetRoleClaims rc
INNER JOIN dbo.AspNetUserRoles ur ON ur.RoleId=rc.RoleId
WHERE ur.UserId=@Id";
    }
}
