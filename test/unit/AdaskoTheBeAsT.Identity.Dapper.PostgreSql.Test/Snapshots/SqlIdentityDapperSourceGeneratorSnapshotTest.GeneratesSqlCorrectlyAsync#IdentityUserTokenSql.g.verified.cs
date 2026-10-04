//HintName: IdentityUserTokenSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserTokenSql : IIdentityUserTokenConcurrencySql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO aspnetusertokens(
                "userid"
               ,"loginprovider"
               ,"name"
               ,"value")
            VALUES(
                @UserId
               ,@LoginProvider
               ,@Name
               ,@Value);
            """;

        public string UpdateSql { get; } =
            """
            UPDATE aspnetusertokens
            SET "value"=@Value
            WHERE "userid"=@UserId
              AND "loginprovider"=@LoginProvider
              AND "name"=@Name
              AND (convert_to("value",'UTF8')=convert_to(@OriginalValue,'UTF8') OR ("value" IS NULL AND @OriginalValue IS NULL));
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM aspnetusertokens
            WHERE "userid"=@UserId
              AND "loginprovider"=@LoginProvider
              AND "name"=@Name;
            """;

        public string GetByUserIdSql { get; } =
            """
            SELECT "userid" AS "UserId"
                  ,"loginprovider" AS "LoginProvider"
                  ,"name" AS "Name"
                  ,"value" AS "Value"
            FROM aspnetusertokens
            WHERE "userid"=@UserId
              AND "loginprovider"=@LoginProvider
              AND "name"=@Name;
            """;
    }
}
