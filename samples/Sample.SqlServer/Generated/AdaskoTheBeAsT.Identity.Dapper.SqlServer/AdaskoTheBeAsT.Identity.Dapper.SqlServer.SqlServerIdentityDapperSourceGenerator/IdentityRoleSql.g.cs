using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace Sample.SqlServer
{
    public class IdentityRoleSql
        : IIdentityRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO AspNetRoles(
[Name]
,[NormalizedName]
,[ConcurrencyStamp])
VALUES(
@Name
,@NormalizedName
,@ConcurrencyStamp)
OUTPUT inserted.Id
VALUES(1);";

        public string UpdateSql { get; } =
            @"UPDATE AspNetRoles
SET [Name]=@Name
,[NormalizedName]=@NormalizedName
,[ConcurrencyStamp]=@ConcurrencyStamp
WHERE Id=@Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM AspNetRoles WHERE Id=@Id;";

        public string FindByIdSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[NormalizedName] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
FROM AspNetRoles
WHERE Id=@Id;";

        public string FindByNameSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[NormalizedName] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
FROM AspNetRoles
WHERE NormalizedName=@NormalizedName;";
    }
}
