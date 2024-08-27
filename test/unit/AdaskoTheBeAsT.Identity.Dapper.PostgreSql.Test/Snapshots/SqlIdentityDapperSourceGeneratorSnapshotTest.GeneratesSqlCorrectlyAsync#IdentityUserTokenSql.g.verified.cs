//HintName: IdentityUserTokenSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserTokenSql
        : IIdentityUserTokenSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetUserTokens(
userid
,loginprovider
,name
,value)
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
            @"SELECT TOP 1 userid  AS ""UserId""
,loginprovider  AS ""LoginProvider""
,name  AS ""Name""
,value  AS ""Value""
FROM dbo.AspNetUserTokens
WHERE UserId=@UserId
  AND LoginProvider=@LoginProvider
  AND Name=@Name;";
    }
}
