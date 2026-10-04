//HintName: IdentityUserRoleClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserRoleClaimSql : IIdentityUserRoleClaimSql
    {
        public string GetRoleClaimsByUserIdSql { get; } =
            """
            SELECT DISTINCT rc."claimtype" AS "Type"
                           ,rc."claimvalue" AS "Value"
            FROM aspnetroleclaims rc INNER JOIN
                 aspnetuserroles ur ON ur."roleid"=rc."roleid"
            WHERE ur."userid"=@Id;
            """;

        public string GetUserAndRoleClaimsByUserIdSql { get; } =
            """
            SELECT uc."claimtype" AS "Type"
                  ,uc."claimvalue" AS "Value"
            FROM aspnetuserclaims uc
            WHERE uc."userid"=@Id
            UNION
            SELECT rc."claimtype" AS "Type"
                  ,rc."claimvalue" AS "Value"
            FROM aspnetroleclaims rc INNER JOIN
                 aspnetuserroles ur ON ur."roleid"=rc."roleid"
            WHERE ur."userid"=@Id;
            """;
    }
}
