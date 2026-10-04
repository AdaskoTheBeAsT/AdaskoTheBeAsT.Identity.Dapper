//HintName: IdentityUserTokenSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserTokenSql : IIdentityUserTokenConcurrencySql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO AspNetUserTokens(
                "USERID"
               ,"LOGINPROVIDER"
               ,"NAME"
               ,"VALUE")
            VALUES(
                :UserId
               ,:LoginProvider
               ,:Name
               ,:Value);
            """;

        public string UpdateSql { get; } =
            """
            UPDATE AspNetUserTokens
            SET "VALUE"=:Value
            WHERE "USERID"=:UserId
              AND "LOGINPROVIDER"=:LoginProvider
              AND "NAME"=:Name
              AND (UTL_RAW.CAST_TO_RAW("VALUE")=UTL_RAW.CAST_TO_RAW(:OriginalValue) OR ("VALUE" IS NULL AND :OriginalValue IS NULL));
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM AspNetUserTokens
            WHERE "USERID"=:UserId
              AND "LOGINPROVIDER"=:LoginProvider
              AND "NAME"=:Name;
            """;

        public string GetByUserIdSql { get; } =
            """
            SELECT "USERID" AS "USERID"
                  ,"LOGINPROVIDER" AS "LOGINPROVIDER"
                  ,"NAME" AS "NAME"
                  ,"VALUE" AS "VALUE"
            FROM AspNetUserTokens
            WHERE "USERID"=:UserId
              AND "LOGINPROVIDER"=:LoginProvider
              AND "NAME"=:Name;
            """;
    }
}
