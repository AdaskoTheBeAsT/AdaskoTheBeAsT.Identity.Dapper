﻿using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity
{
    public class IdentityUserSql
        : IIdentityUserSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetUsers(
[Id]
,[UserName]
,[Email]
,[EmailConfirmed]
,[PasswordHash]
,[SecurityStamp]
,[ConcurrencyStamp]
,[PhoneNumber]
,[PhoneNumberConfirmed]
,[TwoFactorEnabled]
,[LockoutEnd]
,[LockoutEnabled]
,[AccessFailedCount])
OUTPUT inserted.Id
VALUES(
@Id
,@UserName
,@Email
,@EmailConfirmed
,@PasswordHash
,@SecurityStamp
,@ConcurrencyStamp
,@PhoneNumber
,@PhoneNumberConfirmed
,@TwoFactorEnabled
,@LockoutEnd
,@LockoutEnabled
,@AccessFailedCount);";

        public string UpdateSql { get; } =
            @"UPDATE dbo.AspNetUsers
SET [Id]=@Id
,[UserName]=@UserName
,[Email]=@Email
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
WHERE Id=@Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetUsers WHERE Id=@Id;";

        public string FindByIdSql { get; } =
            @"SELECT [Id] AS Id
,[UserName] AS UserName
,[Email] AS Email
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
FROM dbo.AspNetUsers
WHERE Id=@Id;";

        public string FindByNameSql { get; } =
            @"SELECT [Id] AS Id
,[UserName] AS UserName
,[Email] AS Email
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
FROM dbo.AspNetUsers
WHERE UserName=@NormalizedUserName;";

        public string FindByEmailSql { get; } =
            @"SELECT [Id] AS Id
,[UserName] AS UserName
,[Email] AS Email
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
FROM dbo.AspNetUsers
WHERE Email=@NormalizedEmail;";

        public string GetUsersForClaimSql { get; } =
            @"SELECT u.Id
,u.[Id] AS Id
,u.[UserName] AS UserName
,u.[Email] AS Email
,u.[EmailConfirmed] AS EmailConfirmed
,u.[PasswordHash] AS PasswordHash
,u.[SecurityStamp] AS SecurityStamp
,u.[ConcurrencyStamp] AS ConcurrencyStamp
,u.[PhoneNumber] AS PhoneNumber
,u.[PhoneNumberConfirmed] AS PhoneNumberConfirmed
,u.[TwoFactorEnabled] AS TwoFactorEnabled
,u.[LockoutEnd] AS LockoutEnd
,u.[LockoutEnabled] AS LockoutEnabled
,u.[AccessFailedCount] AS AccessFailedCount
FROM dbo.AspNetUsers u
INNER JOIN dbo.AspNetUserClaims c ON u.Id=c.UserId
WHERE c.ClaimType=@ClaimType
  AND c.ClaimValue=@ClaimValue;";

        public string GetUsersInRoleSql { get; } =
            @"SELECT u.Id
,u.[Id] AS Id
,u.[UserName] AS UserName
,u.[Email] AS Email
,u.[EmailConfirmed] AS EmailConfirmed
,u.[PasswordHash] AS PasswordHash
,u.[SecurityStamp] AS SecurityStamp
,u.[ConcurrencyStamp] AS ConcurrencyStamp
,u.[PhoneNumber] AS PhoneNumber
,u.[PhoneNumberConfirmed] AS PhoneNumberConfirmed
,u.[TwoFactorEnabled] AS TwoFactorEnabled
,u.[LockoutEnd] AS LockoutEnd
,u.[LockoutEnabled] AS LockoutEnabled
,u.[AccessFailedCount] AS AccessFailedCount
FROM dbo.AspNetUsers u
INNER JOIN dbo.AspNetUserRoles ur ON u.Id=ur.UserId
INNER JOIN dbo.AspNetRoles r ON ur.RolesId=r.Id
WHERE r.Name=@NormalizedName;";

        public string GetUsersSql { get; } =
            @"SELECT u.Id
,u.[Id] AS Id
,u.[UserName] AS UserName
,u.[Email] AS Email
,u.[EmailConfirmed] AS EmailConfirmed
,u.[PasswordHash] AS PasswordHash
,u.[SecurityStamp] AS SecurityStamp
,u.[ConcurrencyStamp] AS ConcurrencyStamp
,u.[PhoneNumber] AS PhoneNumber
,u.[PhoneNumberConfirmed] AS PhoneNumberConfirmed
,u.[TwoFactorEnabled] AS TwoFactorEnabled
,u.[LockoutEnd] AS LockoutEnd
,u.[LockoutEnabled] AS LockoutEnabled
,u.[AccessFailedCount] AS AccessFailedCount
FROM dbo.AspNetUsers u
INNER JOIN dbo.AspNetUserRoles ur ON u.Id=ur.UserId
INNER JOIN dbo.AspNetRoles r ON ur.RolesId=r.Id
;";
    }
}
