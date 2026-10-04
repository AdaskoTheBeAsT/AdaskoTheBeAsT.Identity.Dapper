//HintName: IdentityUserLoginSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserLoginSql : IIdentityUserLoginSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO aspnetuserlogins(
                "loginprovider"
               ,"providerkey"
               ,"providerdisplayname"
               ,"userid")
            VALUES(
                @LoginProvider
               ,@ProviderKey
               ,@ProviderDisplayName
               ,@UserId);
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM aspnetuserlogins
            WHERE "userid"=@UserId
              AND "loginprovider"=@LoginProvider
              AND "providerkey"=@ProviderKey;
            """;

        public string GetByUserIdSql { get; } =
            """
            SELECT "loginprovider" AS "LoginProvider"
                  ,"providerkey" AS "ProviderKey"
                  ,"providerdisplayname" AS "ProviderDisplayName"
                  ,"userid" AS "UserId"
            FROM aspnetuserlogins
            WHERE "userid"=@Id;
            """;

        public string GetByUserIdLoginProviderKeySql { get; } =
            """
            SELECT "loginprovider" AS "LoginProvider"
                  ,"providerkey" AS "ProviderKey"
                  ,"providerdisplayname" AS "ProviderDisplayName"
                  ,"userid" AS "UserId"
            FROM aspnetuserlogins
            WHERE "userid"=@UserId
              AND "loginprovider"=@LoginProvider
              AND "providerkey"=@ProviderKey;
            """;

        public string GetByLoginProviderKeySql { get; } =
            """
            SELECT "loginprovider" AS "LoginProvider"
                  ,"providerkey" AS "ProviderKey"
                  ,"providerdisplayname" AS "ProviderDisplayName"
                  ,"userid" AS "UserId"
            FROM aspnetuserlogins
            WHERE "loginprovider"=@LoginProvider
              AND "providerkey"=@ProviderKey;
            """;
    }
}
