using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity
{
    public class IdentityRoleSql
        : IIdentityRoleSql
    {
        public string CreateSql { get; } =
            @"DECLARE id AspNetRoles.ID%type;
BEGIN
    id := SYS_GUID();
    INSERT INTO AspNetRoles(
    Id,
    Name
,ConcurrencyStamp)
    VALUES(
    id,
    :Name
,:ConcurrencyStamp)
    RETURNING Id INTO :OutputId;
END;";

        public string UpdateSql { get; } =
            @"UPDATE AspNetRoles
SET Name=:Name
,ConcurrencyStamp=:ConcurrencyStamp
WHERE Id=:Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM AspNetRoles WHERE Id=:Id;";

        public string FindByIdSql { get; } =
            @"SELECT Id
,Name AS Name
,Name AS NormalizedName
,ConcurrencyStamp AS ConcurrencyStamp
FROM AspNetRoles
WHERE Id=:Id;";

        public string FindByNameSql { get; } =
            @"SELECT Id
,Name AS Name
,Name AS NormalizedName
,ConcurrencyStamp AS ConcurrencyStamp
FROM AspNetRoles
WHERE Name=:NormalizedName;";

        public string GetRolesSql { get; } =
            @"SELECT Id
,Name AS Name
,Name AS NormalizedName
,ConcurrencyStamp AS ConcurrencyStamp
FROM AspNetRoles;";
    }
}
