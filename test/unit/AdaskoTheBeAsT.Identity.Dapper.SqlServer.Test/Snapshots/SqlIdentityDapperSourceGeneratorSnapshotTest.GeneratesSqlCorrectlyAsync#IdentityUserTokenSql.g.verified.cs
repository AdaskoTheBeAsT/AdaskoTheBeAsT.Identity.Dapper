//HintName: IdentityUserTokenSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserTokenSql : IIdentityUserTokenConcurrencySql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO AspNetUserTokens(
                [UserId]
               ,[LoginProvider]
               ,[Name]
               ,[Value])
            VALUES(
                @UserId
               ,@LoginProvider
               ,@Name
               ,@Value);
            """;

        public string UpdateSql { get; } =
            """
            UPDATE AspNetUserTokens
            SET [Value]=@Value
            WHERE [UserId]=@UserId
              AND [LoginProvider]=@LoginProvider
              AND [Name]=@Name
              AND (CONVERT(varbinary(max),[Value])=CONVERT(varbinary(max),@OriginalValue) OR ([Value] IS NULL AND @OriginalValue IS NULL));
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM AspNetUserTokens
            WHERE [UserId]=@UserId
              AND [LoginProvider]=@LoginProvider
              AND [Name]=@Name;
            """;

        public string GetByUserIdSql { get; } =
            """
            SELECT [UserId] AS [UserId]
                  ,[LoginProvider] AS [LoginProvider]
                  ,[Name] AS [Name]
                  ,[Value] AS [Value]
            FROM AspNetUserTokens
            WHERE [UserId]=@UserId
              AND [LoginProvider]=@LoginProvider
              AND [Name]=@Name;
            """;
    }
}
