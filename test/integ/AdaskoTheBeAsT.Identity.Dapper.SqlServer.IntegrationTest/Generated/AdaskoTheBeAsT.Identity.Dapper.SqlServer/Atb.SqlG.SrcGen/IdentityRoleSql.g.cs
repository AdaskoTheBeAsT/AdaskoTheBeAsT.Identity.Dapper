using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity
{
    public class IdentityRoleSql
        : IIdentityRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetRoles(
[Name]
,[ConcurrencyStamp])
OUTPUT inserted.Id
VALUES(
@Name
,@ConcurrencyStamp);";

        public string UpdateSql { get; } =
            @"UPDATE dbo.AspNetRoles
SET [Name]=@Name
,[ConcurrencyStamp]=@ConcurrencyStamp
WHERE Id=@Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetRoles WHERE Id=@Id;";

        public string FindByIdSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[Name] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
FROM dbo.AspNetRoles
WHERE Id=@Id;";

        public string FindByNameSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[Name] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
FROM dbo.AspNetRoles
WHERE Name=@NormalizedName;";

        public string GetRolesSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[Name] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
FROM dbo.AspNetRoles;";
    }
}
