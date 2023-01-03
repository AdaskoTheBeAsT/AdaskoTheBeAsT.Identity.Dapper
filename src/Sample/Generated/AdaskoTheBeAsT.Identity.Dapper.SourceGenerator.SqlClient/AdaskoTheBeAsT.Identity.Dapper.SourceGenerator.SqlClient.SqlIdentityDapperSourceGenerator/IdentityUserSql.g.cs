using AdaskoTheBeAsT.Identity.Dapper.IdentitySql;

namespace Sample
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
