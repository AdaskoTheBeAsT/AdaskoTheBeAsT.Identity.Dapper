//HintName: IdentityRoleClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityRoleClaimSql : IIdentityRoleClaimSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO AspNetRoleClaims(
                "ROLEID"
               ,"CLAIMTYPE"
               ,"CLAIMVALUE")
            VALUES(
                :RoleId
               ,:ClaimType
               ,:ClaimValue);
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM AspNetRoleClaims
            WHERE "ROLEID"=:RoleId
              AND "CLAIMTYPE"=:ClaimType
              AND "CLAIMVALUE"=:ClaimValue;
            """;

        public string GetByRoleIdSql { get; } =
            """
            SELECT "CLAIMTYPE" AS "TYPE"
                  ,"CLAIMVALUE" AS "VALUE"
            FROM AspNetRoleClaims
            WHERE "ROLEID"=:Id;
            """;
    }
}
