//HintName: IdentityUserSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserSql
        : IIdentityUserSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetUsers(
username
,normalizedusername
,email
,normalizedemail
,emailconfirmed
,passwordhash
,securitystamp
,concurrencystamp
,phonenumber
,phonenumberconfirmed
,twofactorenabled
,lockoutend
,lockoutenabled
,accessfailedcount)
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
,@AccessFailedCount)
RETURNING Id AS ""Id"";";

        public string UpdateSql { get; } =
            @"UPDATE dbo.AspNetUsers
SET username=@UserName
,normalizedusername=@NormalizedUserName
,email=@Email
,normalizedemail=@NormalizedEmail
,emailconfirmed=@EmailConfirmed
,passwordhash=@PasswordHash
,securitystamp=@SecurityStamp
,concurrencystamp=@ConcurrencyStamp
,phonenumber=@PhoneNumber
,phonenumberconfirmed=@PhoneNumberConfirmed
,twofactorenabled=@TwoFactorEnabled
,lockoutend=@LockoutEnd
,lockoutenabled=@LockoutEnabled
,accessfailedcount=@AccessFailedCount
WHERE Id=@Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetUsers WHERE Id=@Id;";

        public string FindByIdSql { get; } =
            @"SELECT Id  AS ""Id""
,username AS ""UserName""
,normalizedusername AS ""NormalizedUserName""
,email AS ""Email""
,normalizedemail AS ""NormalizedEmail""
,emailconfirmed AS ""EmailConfirmed""
,passwordhash AS ""PasswordHash""
,securitystamp AS ""SecurityStamp""
,concurrencystamp AS ""ConcurrencyStamp""
,phonenumber AS ""PhoneNumber""
,phonenumberconfirmed AS ""PhoneNumberConfirmed""
,twofactorenabled AS ""TwoFactorEnabled""
,lockoutend AS ""LockoutEnd""
,lockoutenabled AS ""LockoutEnabled""
,accessfailedcount AS ""AccessFailedCount""
FROM dbo.AspNetUsers
WHERE Id=@Id;";

        public string FindByNameSql { get; } =
            @"SELECT Id  AS ""Id""
,username AS ""UserName""
,normalizedusername AS ""NormalizedUserName""
,email AS ""Email""
,normalizedemail AS ""NormalizedEmail""
,emailconfirmed AS ""EmailConfirmed""
,passwordhash AS ""PasswordHash""
,securitystamp AS ""SecurityStamp""
,concurrencystamp AS ""ConcurrencyStamp""
,phonenumber AS ""PhoneNumber""
,phonenumberconfirmed AS ""PhoneNumberConfirmed""
,twofactorenabled AS ""TwoFactorEnabled""
,lockoutend AS ""LockoutEnd""
,lockoutenabled AS ""LockoutEnabled""
,accessfailedcount AS ""AccessFailedCount""
FROM dbo.AspNetUsers
WHERE NormalizedUserName=@NormalizedUserName;";

        public string FindByEmailSql { get; } =
            @"SELECT Id  AS ""Id""
,username AS ""UserName""
,normalizedusername AS ""NormalizedUserName""
,email AS ""Email""
,normalizedemail AS ""NormalizedEmail""
,emailconfirmed AS ""EmailConfirmed""
,passwordhash AS ""PasswordHash""
,securitystamp AS ""SecurityStamp""
,concurrencystamp AS ""ConcurrencyStamp""
,phonenumber AS ""PhoneNumber""
,phonenumberconfirmed AS ""PhoneNumberConfirmed""
,twofactorenabled AS ""TwoFactorEnabled""
,lockoutend AS ""LockoutEnd""
,lockoutenabled AS ""LockoutEnabled""
,accessfailedcount AS ""AccessFailedCount""
FROM dbo.AspNetUsers
WHERE NormalizedEmail=@NormalizedEmail;";

        public string GetUsersForClaimSql { get; } =
            @"SELECT Id  AS ""Id""
,u.UserName AS ""UserName""
,u.NormalizedUserName AS ""NormalizedUserName""
,u.Email AS ""Email""
,u.NormalizedEmail AS ""NormalizedEmail""
,u.EmailConfirmed AS ""EmailConfirmed""
,u.PasswordHash AS ""PasswordHash""
,u.SecurityStamp AS ""SecurityStamp""
,u.ConcurrencyStamp AS ""ConcurrencyStamp""
,u.PhoneNumber AS ""PhoneNumber""
,u.PhoneNumberConfirmed AS ""PhoneNumberConfirmed""
,u.TwoFactorEnabled AS ""TwoFactorEnabled""
,u.LockoutEnd AS ""LockoutEnd""
,u.LockoutEnabled AS ""LockoutEnabled""
,u.AccessFailedCount AS ""AccessFailedCount""
FROM dbo.AspNetUsers u
INNER JOIN dbo.AspNetUserClaims c ON u.Id=c.UserId
WHERE c.ClaimType=@ClaimType
  AND c.ClaimValue=@ClaimValue;";

        public string GetUsersInRoleSql { get; } =
            @"SELECT Id  AS ""Id""
,u.UserName AS ""UserName""
,u.NormalizedUserName AS ""NormalizedUserName""
,u.Email AS ""Email""
,u.NormalizedEmail AS ""NormalizedEmail""
,u.EmailConfirmed AS ""EmailConfirmed""
,u.PasswordHash AS ""PasswordHash""
,u.SecurityStamp AS ""SecurityStamp""
,u.ConcurrencyStamp AS ""ConcurrencyStamp""
,u.PhoneNumber AS ""PhoneNumber""
,u.PhoneNumberConfirmed AS ""PhoneNumberConfirmed""
,u.TwoFactorEnabled AS ""TwoFactorEnabled""
,u.LockoutEnd AS ""LockoutEnd""
,u.LockoutEnabled AS ""LockoutEnabled""
,u.AccessFailedCount AS ""AccessFailedCount""
FROM dbo.AspNetUsers u
INNER JOIN dbo.AspNetUserRoles ur ON u.Id=ur.UserId
INNER JOIN dbo.AspNetRoles r ON ur.RolesId=r.Id
WHERE r.NormalizedName=@NormalizedName;";

        public string GetUsersSql { get; } =
            @"SELECT Id  AS ""Id""
,u.UserName AS ""UserName""
,u.NormalizedUserName AS ""NormalizedUserName""
,u.Email AS ""Email""
,u.NormalizedEmail AS ""NormalizedEmail""
,u.EmailConfirmed AS ""EmailConfirmed""
,u.PasswordHash AS ""PasswordHash""
,u.SecurityStamp AS ""SecurityStamp""
,u.ConcurrencyStamp AS ""ConcurrencyStamp""
,u.PhoneNumber AS ""PhoneNumber""
,u.PhoneNumberConfirmed AS ""PhoneNumberConfirmed""
,u.TwoFactorEnabled AS ""TwoFactorEnabled""
,u.LockoutEnd AS ""LockoutEnd""
,u.LockoutEnabled AS ""LockoutEnabled""
,u.AccessFailedCount AS ""AccessFailedCount""
FROM dbo.AspNetUsers u
INNER JOIN dbo.AspNetUserRoles ur ON u.Id=ur.UserId
INNER JOIN dbo.AspNetRoles r ON ur.RolesId=r.Id
;";
    }
}
