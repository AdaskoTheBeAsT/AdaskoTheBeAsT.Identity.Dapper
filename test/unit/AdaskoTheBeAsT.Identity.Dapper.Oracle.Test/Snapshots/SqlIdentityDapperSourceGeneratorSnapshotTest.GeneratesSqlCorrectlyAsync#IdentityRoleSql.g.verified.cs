//HintName: IdentityRoleSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityRoleSql : IIdentityRoleConcurrencySql, IIdentityRolePagingSql
    {
        public string CreateSql { get; } =
            """
            BEGIN
            INSERT INTO AspNetRoles(
                "ID"
               ,"NAME"
               ,"NORMALIZEDNAME"
               ,"CONCURRENCYSTAMP"
               ,"ISACTIVE")
            VALUES(
                SYS_GUID()
               ,:Name
               ,:NormalizedName
               ,:ConcurrencyStamp
               ,:Active)
            RETURNING "ID" INTO :OutputId;
            END;
            """;

        public string UpdateSql { get; } =
            """
            UPDATE AspNetRoles
            SET "NAME"=:Name
               ,"NORMALIZEDNAME"=:NormalizedName
               ,"CONCURRENCYSTAMP"=:ConcurrencyStamp
               ,"ISACTIVE"=:Active
            WHERE "ID"=:Id
              AND ("CONCURRENCYSTAMP"=:OriginalConcurrencyStamp OR ("CONCURRENCYSTAMP" IS NULL AND :OriginalConcurrencyStamp IS NULL));
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM AspNetRoles
            WHERE "ID"=:Id
              AND ("CONCURRENCYSTAMP"=:ConcurrencyStamp OR ("CONCURRENCYSTAMP" IS NULL AND :ConcurrencyStamp IS NULL));
            """;

        public string FindByIdSql { get; } =
            """
            SELECT "ID" AS "ID"
                  ,"NAME" AS "NAME"
                  ,"NORMALIZEDNAME" AS "NORMALIZEDNAME"
                  ,"CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,"ISACTIVE" AS "ACTIVE"
            FROM AspNetRoles
            WHERE "ID"=:Id;
            """;

        public string FindByNameSql { get; } =
            """
            SELECT "ID" AS "ID"
                  ,"NAME" AS "NAME"
                  ,"NORMALIZEDNAME" AS "NORMALIZEDNAME"
                  ,"CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,"ISACTIVE" AS "ACTIVE"
            FROM AspNetRoles
            WHERE "NORMALIZEDNAME"=:NormalizedName;
            """;

        public string GetRolesSql { get; } =
            """
            SELECT "ID" AS "ID"
                  ,"NAME" AS "NAME"
                  ,"NORMALIZEDNAME" AS "NORMALIZEDNAME"
                  ,"CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,"ISACTIVE" AS "ACTIVE"
            FROM AspNetRoles;
            """;

        public string GetRolesPageSql { get; } =
            """
            SELECT "ID" AS "ID"
                  ,"NAME" AS "NAME"
                  ,"NORMALIZEDNAME" AS "NORMALIZEDNAME"
                  ,"CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,"ISACTIVE" AS "ACTIVE"
            FROM AspNetRoles
            ORDER BY "ID"
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """;
    }
}
