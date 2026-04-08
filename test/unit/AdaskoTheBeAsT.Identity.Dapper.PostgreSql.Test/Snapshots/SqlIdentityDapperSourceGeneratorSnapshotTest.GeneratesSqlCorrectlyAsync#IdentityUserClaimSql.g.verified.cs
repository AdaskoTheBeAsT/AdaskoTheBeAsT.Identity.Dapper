//HintName: IdentityUserClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserClaimSql
        : IIdentityUserClaimSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO AspNetUserClaims(
userid
,claimtype
,claimvalue)
VALUES(
@UserId
,@ClaimType
,@ClaimValue);
SELECT LASTVAL() AS Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM AspNetUserClaims
WHERE UserId=@UserId
  AND ClaimType=@ClaimType
  AND ClaimValue=@ClaimValue;";

        public string GetByUserIdSql { get; } =
            @"SELECT ClaimType AS ""Type"",
ClaimValue AS ""Value""
FROM AspNetUserClaims
WHERE UserId=@Id;";

        public string ReplaceSql { get; } =
            @"DELETE FROM AspNetUserClaims
WHERE UserId=@UserId
  AND ClaimType=@ClaimTypeOld
  AND ClaimValue=@ClaimValueOld;
INSERT INTO AspNetUserClaims(
userid
,claimtype
,claimvalue)
SELECT
@UserId
,@ClaimType
,@ClaimValue
WHERE NOT EXISTS(
    SELECT 1
    FROM AspNetUserClaims
    WHERE UserId=@UserId
      AND ClaimType=@ClaimTypeNew
      AND ClaimValue=@ClaimValueNew);";
    }
}
