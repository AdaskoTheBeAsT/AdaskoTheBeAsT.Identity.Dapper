//HintName: IdentityUserClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserClaimSql : IIdentityUserClaimBatchSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO AspNetUserClaims(
                "USERID"
               ,"CLAIMTYPE"
               ,"CLAIMVALUE")
            VALUES(
                :UserId
               ,:ClaimType
               ,:ClaimValue);
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM AspNetUserClaims
            WHERE "USERID"=:UserId
              AND "CLAIMTYPE"=:ClaimType
              AND "CLAIMVALUE"=:ClaimValue;
            """;

        public string GetByUserIdSql { get; } =
            """
            SELECT "CLAIMTYPE" AS "TYPE"
                  ,"CLAIMVALUE" AS "VALUE"
            FROM AspNetUserClaims
            WHERE "USERID"=:Id;
            """;

        public string ReplaceSql { get; } =
            """
            UPDATE AspNetUserClaims
            SET "CLAIMTYPE"=:ClaimTypeNew
               ,"CLAIMVALUE"=:ClaimValueNew
            WHERE "USERID"=:UserId
              AND "CLAIMTYPE"=:ClaimTypeOld
              AND "CLAIMVALUE"=:ClaimValueOld;
            """;

        public string CreateBatchItemSql { get; } =
            """
            INSERT INTO AspNetUserClaims(
                "USERID"
               ,"CLAIMTYPE"
               ,"CLAIMVALUE")
            VALUES(
                :UserId_{0}
               ,:ClaimType_{0}
               ,:ClaimValue_{0});
            """;

        public string DeleteBatchItemSql { get; } =
            """
            DELETE FROM AspNetUserClaims
            WHERE "USERID"=:UserId_{0}
              AND "CLAIMTYPE"=:ClaimType_{0}
              AND "CLAIMVALUE"=:ClaimValue_{0};
            """;

        public System.Collections.Generic.IReadOnlyList<string> CreateBatchParameterNames { get; } = new[] { "UserId", "ClaimType", "ClaimValue" };

        public string BatchPrefix { get; } = "BEGIN\n";

        public string BatchSuffix { get; } = "\nEND;";
    }
}
