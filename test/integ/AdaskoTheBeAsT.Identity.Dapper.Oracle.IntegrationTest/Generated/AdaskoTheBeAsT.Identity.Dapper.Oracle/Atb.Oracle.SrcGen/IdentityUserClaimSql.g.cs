using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity
{
    public class IdentityUserClaimSql
        : IIdentityUserClaimSql
    {
        public string CreateSql { get; } =
            @"DECLARE id AspNetUserClaims.Id%type;
INSERT INTO AspNetUserClaims(
UserId
,ClaimType
,ClaimValue)
VALUES(
:UserId
,:ClaimType
,:ClaimValue)
RETURNING Id INTO id;
SELECT id FROM DUAL;
";

        public string DeleteSql { get; } =
            @"DELETE FROM AspNetUserClaims
WHERE UserId=:UserId
  AND ClaimType=:ClaimType
  AND ClaimValue=:ClaimValue;";

        public string GetByUserIdSql { get; } =
            @"SELECT ClaimType AS Type,
ClaimValue AS Value
FROM AspNetUserClaims
WHERE UserId=:Id;";

        public string ReplaceSql { get; } =
            @"IF EXISTS(SELECT Id
            FROM AspNetUserClaims
            WHERE UserId=:UserId
              AND ClaimType=:ClaimTypeOld
              AND ClaimValue=:ClaimValueOld)
BEGIN
    DELETE FROM AspNetUserClaims
    WHERE UserId=:UserId
      AND ClaimType=:ClaimTypeOld
      AND ClaimValue=:ClaimValueOld
END;
IF NOT EXISTS(SELECT Id
             FROM AspNetUserClaims
             WHERE UserId=:UserId
               AND ClaimType=:ClaimTypeNew
               AND ClaimValue=:ClaimValueNew)
BEGIN
    INSERT INTO AspNetUserClaims(
UserId
,ClaimType
,ClaimValue)
VALUES(
:UserId
,:ClaimType
,:ClaimValue);
END;";
    }
}
