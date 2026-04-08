//HintName: IdentityUserClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserClaimSql
        : IIdentityUserClaimSql
    {
        public string CreateSql { get; } =
            @"BEGIN
INSERT INTO AspNetUserClaims(
UserId
,ClaimType
,ClaimValue)
VALUES(
:UserId
,:ClaimType
,:ClaimValue);
END;
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
            @"BEGIN
    DELETE FROM AspNetUserClaims
    WHERE UserId=:UserId
      AND ClaimType=:ClaimTypeOld
      AND ClaimValue=:ClaimValueOld;
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
