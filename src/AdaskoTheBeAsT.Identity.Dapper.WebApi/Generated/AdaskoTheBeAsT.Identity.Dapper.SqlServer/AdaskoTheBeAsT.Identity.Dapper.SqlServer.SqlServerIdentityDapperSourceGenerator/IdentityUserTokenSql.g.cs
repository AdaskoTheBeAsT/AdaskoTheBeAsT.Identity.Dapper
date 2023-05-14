using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity
{
    public class IdentityUserTokenSql
        : IIdentityUserTokenSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetUserTokens(
[UserId]
,[LoginProvider]
,[Name]
,[Value])
VALUES(
@UserId
,@LoginProvider
,@Name
,@Value);";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetUserTokens
WHERE LoginProvider=@LoginProvider
  AND Name=@Name
  AND Value=@Value
  AND UserId=@UserId;";

        public string GetByUserIdSql { get; } =
            @"SELECT TOP 1 [UserId] AS UserId
,[LoginProvider] AS LoginProvider
,[Name] AS Name
,[Value] AS Value
FROM dbo.AspNetUserTokens
WHERE UserId=@UserId
  AND LoginProvider=@LoginProvider
  AND Name=@Name;";
    }
}
