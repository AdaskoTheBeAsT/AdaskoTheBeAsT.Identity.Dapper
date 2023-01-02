//HintName: IdentityUserSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.IdentitySql;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserSql
        : IIdentityUserSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetUsers(IsActive) 
VALUES(@Active);
SELECT SCOPE_IDENTITY();";
    }
}
