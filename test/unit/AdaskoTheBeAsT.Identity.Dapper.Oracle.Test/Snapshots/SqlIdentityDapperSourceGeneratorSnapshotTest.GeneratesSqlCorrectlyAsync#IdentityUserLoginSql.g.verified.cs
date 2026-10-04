//HintName: IdentityUserLoginSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserLoginSql : IIdentityUserLoginSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO AspNetUserLogins(
                "LOGINPROVIDER"
               ,"PROVIDERKEY"
               ,"PROVIDERDISPLAYNAME"
               ,"USERID")
            VALUES(
                :LoginProvider
               ,:ProviderKey
               ,:ProviderDisplayName
               ,:UserId);
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM AspNetUserLogins
            WHERE "USERID"=:UserId
              AND "LOGINPROVIDER"=:LoginProvider
              AND "PROVIDERKEY"=:ProviderKey;
            """;

        public string GetByUserIdSql { get; } =
            """
            SELECT "LOGINPROVIDER" AS "LOGINPROVIDER"
                  ,"PROVIDERKEY" AS "PROVIDERKEY"
                  ,"PROVIDERDISPLAYNAME" AS "PROVIDERDISPLAYNAME"
                  ,"USERID" AS "USERID"
            FROM AspNetUserLogins
            WHERE "USERID"=:Id;
            """;

        public string GetByUserIdLoginProviderKeySql { get; } =
            """
            SELECT "LOGINPROVIDER" AS "LOGINPROVIDER"
                  ,"PROVIDERKEY" AS "PROVIDERKEY"
                  ,"PROVIDERDISPLAYNAME" AS "PROVIDERDISPLAYNAME"
                  ,"USERID" AS "USERID"
            FROM AspNetUserLogins
            WHERE "USERID"=:UserId
              AND "LOGINPROVIDER"=:LoginProvider
              AND "PROVIDERKEY"=:ProviderKey;
            """;

        public string GetByLoginProviderKeySql { get; } =
            """
            SELECT "LOGINPROVIDER" AS "LOGINPROVIDER"
                  ,"PROVIDERKEY" AS "PROVIDERKEY"
                  ,"PROVIDERDISPLAYNAME" AS "PROVIDERDISPLAYNAME"
                  ,"USERID" AS "USERID"
            FROM AspNetUserLogins
            WHERE "LOGINPROVIDER"=:LoginProvider
              AND "PROVIDERKEY"=:ProviderKey;
            """;
    }
}
