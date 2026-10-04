//HintName: IdentityUserClaimSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserClaimSql : IIdentityUserClaimBatchSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO AspNetUserClaims(
                [UserId]
               ,[ClaimType]
               ,[ClaimValue])
            VALUES(
                @UserId
               ,@ClaimType
               ,@ClaimValue);
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM AspNetUserClaims
            WHERE [UserId]=@UserId
              AND [ClaimType]=@ClaimType
              AND [ClaimValue]=@ClaimValue;
            """;

        public string GetByUserIdSql { get; } =
            """
            SELECT [ClaimType] AS [Type]
                  ,[ClaimValue] AS [Value]
            FROM AspNetUserClaims
            WHERE [UserId]=@Id;
            """;

        public string ReplaceSql { get; } =
            """
            UPDATE AspNetUserClaims
            SET [ClaimType]=@ClaimTypeNew
               ,[ClaimValue]=@ClaimValueNew
            WHERE [UserId]=@UserId
              AND [ClaimType]=@ClaimTypeOld
              AND [ClaimValue]=@ClaimValueOld;
            """;

        public string CreateBatchItemSql { get; } =
            """
            INSERT INTO AspNetUserClaims(
                [UserId]
               ,[ClaimType]
               ,[ClaimValue])
            VALUES(
                @UserId_{0}
               ,@ClaimType_{0}
               ,@ClaimValue_{0});
            """;

        public string DeleteBatchItemSql { get; } =
            """
            DELETE FROM AspNetUserClaims
            WHERE [UserId]=@UserId_{0}
              AND [ClaimType]=@ClaimType_{0}
              AND [ClaimValue]=@ClaimValue_{0};
            """;

        public System.Collections.Generic.IReadOnlyList<string> CreateBatchParameterNames { get; } = new[] { "UserId", "ClaimType", "ClaimValue" };

        public string BatchPrefix { get; } = "";

        public string BatchSuffix { get; } = "";
    }
}
