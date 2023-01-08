using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace Sample
{
    public class IdentityRoleSql
        : IIdentityRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetRoles(
[Name]
,[NormalizedName]
,[ConcurrencyStamp])
VALUES(
@Name
,@NormalizedName
,@ConcurrencyStamp);
SELECT SCOPE_IDENTITY();";

        public string UpdateSql { get; } =
            @"UPDATE dbo.AspNetRoles
SET [Name]=@Name
,[NormalizedName]=@NormalizedName
,[ConcurrencyStamp]=@ConcurrencyStamp
WHERE Id=@Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetRoles WHERE Id=@Id;";

        public string FindByIdSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[NormalizedName] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
FROM dbo.AspNetRoles
WHERE Id=@Id;";

        public string FindByNameSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[NormalizedName] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
FROM dbo.AspNetRoles
WHERE NormalizedName=@NormalizedName;";
    }
}
