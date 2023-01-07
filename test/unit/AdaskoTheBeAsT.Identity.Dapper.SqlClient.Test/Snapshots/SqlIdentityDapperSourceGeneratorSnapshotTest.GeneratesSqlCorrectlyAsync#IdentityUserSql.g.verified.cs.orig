//HintName: IdentityUserSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.IdentitySql;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserSql
        : IIdentityUserSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetUsers(
[UserName]
,[NormalizedUserName]
,[Email]
,[NormalizedEmail]
,[EmailConfirmed]
,[PasswordHash]
,[SecurityStamp]
,[ConcurrencyStamp]
,[PhoneNumber]
,[PhoneNumberConfirmed]
,[TwoFactorEnabled]
,[LockoutEnd]
,[LockoutEnabled]
,[AccessFailedCount]
,[IsActive])
VALUES(
@UserName
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

        public string UpdateSql { get; } =
            @"UPDATE dbo.AspNetUsers
SET [UserName]=@UserName
,[NormalizedUserName]=@NormalizedUserName
,[Email]=@Email
,[NormalizedEmail]=@NormalizedEmail
,[EmailConfirmed]=@EmailConfirmed
,[PasswordHash]=@PasswordHash
,[SecurityStamp]=@SecurityStamp
,[ConcurrencyStamp]=@ConcurrencyStamp
,[PhoneNumber]=@PhoneNumber
,[PhoneNumberConfirmed]=@PhoneNumberConfirmed
,[TwoFactorEnabled]=@TwoFactorEnabled
,[LockoutEnd]=@LockoutEnd
,[LockoutEnabled]=@LockoutEnabled
,[AccessFailedCount]=@AccessFailedCount
,[IsActive]=@Active
WHERE Id=@Id
;";
    }
}
