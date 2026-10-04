//HintName: IdentityUserRoleSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserRoleSql : IIdentityUserRoleSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO AspNetUserRoles(
                "USERID"
               ,"ROLEID")
            VALUES(
                :UserId
               ,:RoleId);
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM AspNetUserRoles
            WHERE "USERID"=:UserId
              AND "ROLEID"=:RoleId;
            """;

        public string GetByUserIdRoleIdSql { get; } =
            """
            SELECT "USERID" AS "USERID"
                  ,"ROLEID" AS "ROLEID"
            FROM AspNetUserRoles
            WHERE "USERID"=:UserId
              AND "ROLEID"=:RoleId;
            """;

        public string GetCountSql { get; } =
            """
            SELECT COUNT(*)
            FROM AspNetUserRoles
            WHERE "USERID"=:UserId
              AND "ROLEID"=:RoleId;
            """;

        public string GetRoleNamesByUserIdSql { get; } =
            """
            SELECT r."NORMALIZEDNAME"
            FROM AspNetRoles r INNER JOIN
                 AspNetUserRoles ur ON r."ID"=ur."ROLEID"
            WHERE ur."USERID"=:UserId;
            """;
    }
}
