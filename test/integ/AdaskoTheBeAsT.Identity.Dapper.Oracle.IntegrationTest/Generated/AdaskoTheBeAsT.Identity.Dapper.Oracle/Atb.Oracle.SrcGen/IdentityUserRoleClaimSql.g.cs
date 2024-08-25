using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity
{
    public class IdentityUserRoleClaimSql
        : IIdentityUserRoleClaimSql
    {
        public string GetRoleClaimsByUserIdSql { get; } =
            @"SELECT DISTINCT rc.ClaimType AS Type,
rc.ClaimValue AS Value
FROM AspNetRoleClaims rc
INNER JOIN AspNetUserRoles ur ON ur.RoleId=rc.RoleId
WHERE ur.UserId=:Id;";

        public string GetUserAndRoleClaimsByUserIdSql { get; } =
            @"SELECT uc.ClaimType AS Type
      ,uc.ClaimValue AS Value
FROM AspNetUserClaims uc
WHERE uc.UserId=:Id
UNION
SELECT rc.ClaimType AS Type
      ,rc.ClaimValue AS Value
FROM AspNetRoleClaims rc
INNER JOIN AspNetUserRoles ur ON ur.RoleId=rc.RoleId
WHERE ur.UserId=:Id";
    }
}
