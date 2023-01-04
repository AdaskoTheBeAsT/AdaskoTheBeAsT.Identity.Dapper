//HintName: IdentityUserSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.IdentitySql;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserSql
        : IIdentityUserSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetUsers(UserName
,NormalizedUserName
,Email
,NormalizedEmail
,EmailConfirmed
,PasswordHash
,SecurityStamp
,ConcurrencyStamp
,PhoneNumber
,PhoneNumberConfirmed
,TwoFactorEnabled
,LockoutEnd
,LockoutEnabled
,AccessFailedCount
,IsActive)
VALUES(@UserName
,@NormalizedUserName
,@Email
,@NormalizedEmail
,@EmailConfirmed
,@PasswordHash
,@SecurityStamp
,@ConcurrencyStamp
,@PhoneNumber
,@PhoneNumberConfirmed
,@TwoFactorEnabled
,@LockoutEnd
,@LockoutEnabled
,@AccessFailedCount
,@Active);
SELECT SCOPE_IDENTITY();";
    }
}
