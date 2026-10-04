//HintName: IdentityUserRoleSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserRoleSql : IIdentityUserRoleSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO aspnetuserroles(
                "userid"
               ,"roleid")
            VALUES(
                @UserId
               ,@RoleId);
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM aspnetuserroles
            WHERE "userid"=@UserId
              AND "roleid"=@RoleId;
            """;

        public string GetByUserIdRoleIdSql { get; } =
            """
            SELECT "userid" AS "UserId"
                  ,"roleid" AS "RoleId"
            FROM aspnetuserroles
            WHERE "userid"=@UserId
              AND "roleid"=@RoleId;
            """;

        public string GetCountSql { get; } =
            """
            SELECT COUNT(*)
            FROM aspnetuserroles
            WHERE "userid"=@UserId
              AND "roleid"=@RoleId;
            """;

        public string GetRoleNamesByUserIdSql { get; } =
            """
            SELECT r."normalizedname"
            FROM aspnetroles r INNER JOIN
                 aspnetuserroles ur ON r."id"=ur."roleid"
            WHERE ur."userid"=@UserId;
            """;
    }
}
