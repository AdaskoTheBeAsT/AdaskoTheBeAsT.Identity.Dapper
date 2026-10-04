//HintName: IdentityRoleSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityRoleSql : IIdentityRoleConcurrencySql, IIdentityRolePagingSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO aspnetroles(
                "id"
               ,"name"
               ,"normalizedname"
               ,"concurrencystamp"
               ,"isactive")
            VALUES(
                gen_random_uuid()
               ,@Name
               ,@NormalizedName
               ,@ConcurrencyStamp
               ,@Active)
            RETURNING "id" AS "Id";
            """;

        public string UpdateSql { get; } =
            """
            UPDATE aspnetroles
            SET "name"=@Name
               ,"normalizedname"=@NormalizedName
               ,"concurrencystamp"=@ConcurrencyStamp
               ,"isactive"=@Active
            WHERE "id"=@Id
              AND ("concurrencystamp"=@OriginalConcurrencyStamp OR ("concurrencystamp" IS NULL AND @OriginalConcurrencyStamp IS NULL));
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM aspnetroles
            WHERE "id"=@Id
              AND ("concurrencystamp"=@ConcurrencyStamp OR ("concurrencystamp" IS NULL AND @ConcurrencyStamp IS NULL));
            """;

        public string FindByIdSql { get; } =
            """
            SELECT "id" AS "Id"
                  ,"name" AS "Name"
                  ,"normalizedname" AS "NormalizedName"
                  ,"concurrencystamp" AS "ConcurrencyStamp"
                  ,"isactive" AS "Active"
            FROM aspnetroles
            WHERE "id"=@Id;
            """;

        public string FindByNameSql { get; } =
            """
            SELECT "id" AS "Id"
                  ,"name" AS "Name"
                  ,"normalizedname" AS "NormalizedName"
                  ,"concurrencystamp" AS "ConcurrencyStamp"
                  ,"isactive" AS "Active"
            FROM aspnetroles
            WHERE "normalizedname"=@NormalizedName;
            """;

        public string GetRolesSql { get; } =
            """
            SELECT "id" AS "Id"
                  ,"name" AS "Name"
                  ,"normalizedname" AS "NormalizedName"
                  ,"concurrencystamp" AS "ConcurrencyStamp"
                  ,"isactive" AS "Active"
            FROM aspnetroles;
            """;

        public string GetRolesPageSql { get; } =
            """
            SELECT "id" AS "Id"
                  ,"name" AS "Name"
                  ,"normalizedname" AS "NormalizedName"
                  ,"concurrencystamp" AS "ConcurrencyStamp"
                  ,"isactive" AS "Active"
            FROM aspnetroles
            ORDER BY "id"
            LIMIT @PageSize OFFSET @Offset;
            """;
    }
}
