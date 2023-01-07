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
        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetUsers WHERE Id=@Id;";
        public string FindByIdSql { get; } =
            @"SELECT [UserName] AS UserName
,[NormalizedUserName] AS NormalizedUserName
,[Email] AS Email
,[NormalizedEmail] AS NormalizedEmail
,[EmailConfirmed] AS EmailConfirmed
,[PasswordHash] AS PasswordHash
,[SecurityStamp] AS SecurityStamp
,[ConcurrencyStamp] AS ConcurrencyStamp
,[PhoneNumber] AS PhoneNumber
,[PhoneNumberConfirmed] AS PhoneNumberConfirmed
,[TwoFactorEnabled] AS TwoFactorEnabled
,[LockoutEnd] AS LockoutEnd
,[LockoutEnabled] AS LockoutEnabled
,[AccessFailedCount] AS AccessFailedCount
,[IsActive] AS Active

FROM dbo.AspNetUsersWHERE Id=@Id
;";
    }
}
