//HintName: IdentityUserClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserClaimSql : IIdentityUserClaimBatchSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO aspnetuserclaims(
                "userid"
               ,"claimtype"
               ,"claimvalue")
            VALUES(
                @UserId
               ,@ClaimType
               ,@ClaimValue);
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM aspnetuserclaims
            WHERE "userid"=@UserId
              AND "claimtype"=@ClaimType
              AND "claimvalue"=@ClaimValue;
            """;

        public string GetByUserIdSql { get; } =
            """
            SELECT "claimtype" AS "Type"
                  ,"claimvalue" AS "Value"
            FROM aspnetuserclaims
            WHERE "userid"=@Id;
            """;

        public string ReplaceSql { get; } =
            """
            UPDATE aspnetuserclaims
            SET "claimtype"=@ClaimTypeNew
               ,"claimvalue"=@ClaimValueNew
            WHERE "userid"=@UserId
              AND "claimtype"=@ClaimTypeOld
              AND "claimvalue"=@ClaimValueOld;
            """;

        public string CreateBatchItemSql { get; } =
            """
            INSERT INTO aspnetuserclaims(
                "userid"
               ,"claimtype"
               ,"claimvalue")
            VALUES(
                @UserId_{0}
               ,@ClaimType_{0}
               ,@ClaimValue_{0});
            """;

        public string DeleteBatchItemSql { get; } =
            """
            DELETE FROM aspnetuserclaims
            WHERE "userid"=@UserId_{0}
              AND "claimtype"=@ClaimType_{0}
              AND "claimvalue"=@ClaimValue_{0};
            """;

        public System.Collections.Generic.IReadOnlyList<string> CreateBatchParameterNames { get; } = new[] { "UserId", "ClaimType", "ClaimValue" };

        public string BatchPrefix { get; } = "";

        public string BatchSuffix { get; } = "";
    }
}
