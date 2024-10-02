//HintName: IdentityUserLoginSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserLoginSql
        : IIdentityUserLoginSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO AspNetUserLogins(
LoginProvider
,ProviderKey
,ProviderDisplayName
,UserId)
VALUES(
:LoginProvider
,:ProviderKey
,:ProviderDisplayName
,:UserId);";

        public string DeleteSql { get; } =
            @"DELETE FROM AspNetUserLogins
WHERE LoginProvider=:LoginProvider
  AND ProviderKey=:ProviderKey
  AND UserId=:UserId;";

        public string GetByUserIdSql { get; } =
            @"SELECT LoginProvider AS LoginProvider
,ProviderKey AS ProviderKey
,ProviderDisplayName AS ProviderDisplayName
,UserId AS UserId
FROM AspNetUserLogins
WHERE UserId=:Id;";

        public string GetByUserIdLoginProviderKeySql { get; } =
            @"SELECT LoginProvider AS LoginProvider
,ProviderKey AS ProviderKey
,ProviderDisplayName AS ProviderDisplayName
,UserId AS UserId
FROM AspNetUserLogins
WHERE UserId=:Id
  AND LoginProvider=:LoginProvider
  AND ProviderKey=:ProviderKey;";

        public string GetByLoginProviderKeySql { get; } =
            @"SELECT LoginProvider AS LoginProvider
,ProviderKey AS ProviderKey
,ProviderDisplayName AS ProviderDisplayName
,UserId AS UserId
FROM AspNetUserLogins
WHERE LoginProvider=:LoginProvider
  AND ProviderKey=:ProviderKey;";
    }
}
