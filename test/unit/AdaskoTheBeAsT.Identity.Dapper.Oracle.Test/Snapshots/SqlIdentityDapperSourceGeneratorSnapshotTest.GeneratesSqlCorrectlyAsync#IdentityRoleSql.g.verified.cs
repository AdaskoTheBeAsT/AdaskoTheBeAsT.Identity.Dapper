﻿//HintName: IdentityRoleSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityRoleSql
        : IIdentityRoleSql
    {
        public string CreateSql { get; } =
            @"DECLARE id dbo.AspNetRoles.ID%type;
BEGIN
    id := SYS_GUID();
    INSERT INTO dbo.AspNetRoles(
    Id,
    Name
,NormalizedName
,ConcurrencyStamp)
    VALUES(
    id,
    :Name
,:NormalizedName
,:ConcurrencyStamp)
    RETURNING Id INTO :OutputId;
END;";

        public string UpdateSql { get; } =
            @"UPDATE dbo.AspNetRoles
SET Name=:Name
,NormalizedName=:NormalizedName
,ConcurrencyStamp=:ConcurrencyStamp
WHERE Id=:Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetRoles WHERE Id=:Id;";

        public string FindByIdSql { get; } =
            @"SELECT Id
,Name AS Name
,NormalizedName AS NormalizedName
,ConcurrencyStamp AS ConcurrencyStamp
FROM dbo.AspNetRoles
WHERE Id=:Id;";

        public string FindByNameSql { get; } =
            @"SELECT Id
,Name AS Name
,NormalizedName AS NormalizedName
,ConcurrencyStamp AS ConcurrencyStamp
FROM dbo.AspNetRoles
WHERE NormalizedName=:NormalizedName;";

        public string GetRolesSql { get; } =
            @"SELECT Id
,Name AS Name
,NormalizedName AS NormalizedName
,ConcurrencyStamp AS ConcurrencyStamp
FROM dbo.AspNetRoles;";
    }
}