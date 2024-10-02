using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace Sample.SqlServer2
{
    public class IdentityUserClaimSql
        : IIdentityUserClaimSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO id.AspNetUserClaims(
[UserId]
,[ClaimType]
,[ClaimValue])
VALUES(
@UserId
,@ClaimType
,@ClaimValue);
SELECT SCOPE_IDENTITY();";

        public string DeleteSql { get; } =
            @"DELETE FROM id.AspNetUserClaims
WHERE UserId=@UserId
  AND ClaimType=@ClaimType
  AND ClaimValue=@ClaimValue;";

        public string GetByUserIdSql { get; } =
            @"SELECT ClaimType AS Type,
ClaimValue AS Value
FROM id.AspNetUserClaims
WHERE UserId=@Id;";

        public string ReplaceSql { get; } =
            @"IF EXISTS(SELECT Id
            FROM id.AspNetUserClaims
            WHERE UserId=@UserId
              AND ClaimType=@ClaimTypeOld
              AND ClaimValue=@ClaimValueOld)
BEGIN
    DELETE FROM id.AspNetUserClaims
    WHERE UserId=@UserId
      AND ClaimType=@ClaimTypeOld
      AND ClaimValue=@ClaimValueOld
END;
IF NOT EXISTS(SELECT Id
             FROM id.AspNetUserClaims
             WHERE UserId=@UserId
               AND ClaimType=@ClaimTypeNew
               AND ClaimValue=@ClaimValueNew)
BEGIN
    INSERT INTO id.AspNetUserClaims(
[UserId]
,[ClaimType]
,[ClaimValue])
VALUES(
@UserId
,@ClaimType
,@ClaimValue);
END;";
    }
}
