//HintName: IdentityUserLoginSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserLoginSql
        : IIdentityUserLoginSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO AspNetUserLogins(
loginprovider
,providerkey
,providerdisplayname
,userid)
VALUES(
@LoginProvider
,@ProviderKey
,@ProviderDisplayName
,@UserId);";

        public string DeleteSql { get; } =
            @"DELETE FROM AspNetUserLogins
WHERE LoginProvider=@LoginProvider
  AND ProviderKey=@ProviderKey
  AND UserId=@UserId;";

        public string GetByUserIdSql { get; } =
            @"SELECT loginprovider AS ""LoginProvider""
,providerkey AS ""ProviderKey""
,providerdisplayname AS ""ProviderDisplayName""
,userid AS ""UserId""
FROM AspNetUserLogins
WHERE UserId=@Id;";

        public string GetByUserIdLoginProviderKeySql { get; } =
            @"SELECT loginprovider AS ""LoginProvider""
,providerkey AS ""ProviderKey""
,providerdisplayname AS ""ProviderDisplayName""
,userid AS ""UserId""
FROM AspNetUserLogins
WHERE UserId=@Id
  AND LoginProvider=@LoginProvider
  AND ProviderKey=@ProviderKey;";

        public string GetByLoginProviderKeySql { get; } =
            @"SELECT loginprovider AS ""LoginProvider""
,providerkey AS ""ProviderKey""
,providerdisplayname AS ""ProviderDisplayName""
,userid AS ""UserId""
FROM AspNetUserLogins
WHERE LoginProvider=@LoginProvider
  AND ProviderKey=@ProviderKey;";
    }
}
