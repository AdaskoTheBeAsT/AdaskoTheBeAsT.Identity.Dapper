//HintName: IdentityUserRoleClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserRoleClaimSql : IIdentityUserRoleClaimSql
    {
        public string GetRoleClaimsByUserIdSql { get; } =
            """
            SELECT DISTINCT rc."CLAIMTYPE" AS "TYPE"
                           ,rc."CLAIMVALUE" AS "VALUE"
            FROM AspNetRoleClaims rc INNER JOIN
                 AspNetUserRoles ur ON ur."ROLEID"=rc."ROLEID"
            WHERE ur."USERID"=:Id;
            """;

        public string GetUserAndRoleClaimsByUserIdSql { get; } =
            """
            SELECT uc."CLAIMTYPE" AS "TYPE"
                  ,uc."CLAIMVALUE" AS "VALUE"
            FROM AspNetUserClaims uc
            WHERE uc."USERID"=:Id
            UNION
            SELECT rc."CLAIMTYPE" AS "TYPE"
                  ,rc."CLAIMVALUE" AS "VALUE"
            FROM AspNetRoleClaims rc INNER JOIN
                 AspNetUserRoles ur ON ur."ROLEID"=rc."ROLEID"
            WHERE ur."USERID"=:Id;
            """;
    }
}
