//HintName: IdentityRoleClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityRoleClaimSql : IIdentityRoleClaimSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO aspnetroleclaims(
                "roleid"
               ,"claimtype"
               ,"claimvalue")
            VALUES(
                @RoleId
               ,@ClaimType
               ,@ClaimValue);
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM aspnetroleclaims
            WHERE "roleid"=@RoleId
              AND "claimtype"=@ClaimType
              AND "claimvalue"=@ClaimValue;
            """;

        public string GetByRoleIdSql { get; } =
            """
            SELECT "claimtype" AS "Type"
                  ,"claimvalue" AS "Value"
            FROM aspnetroleclaims
            WHERE "roleid"=@Id;
            """;
    }
}
