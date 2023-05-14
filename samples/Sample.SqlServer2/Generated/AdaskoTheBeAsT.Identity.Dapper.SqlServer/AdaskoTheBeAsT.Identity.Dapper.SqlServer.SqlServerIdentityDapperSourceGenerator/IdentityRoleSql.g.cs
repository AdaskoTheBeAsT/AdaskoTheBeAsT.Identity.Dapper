using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace Sample.SqlServer2
{
    public class IdentityRoleSql
        : IIdentityRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO id.AspNetRoles(
[Name]
,[ConcurrencyStamp])
OUTPUT inserted.Id
VALUES(
@Name
,@ConcurrencyStamp);";

        public string UpdateSql { get; } =
            @"UPDATE id.AspNetRoles
SET [Name]=@Name
,[ConcurrencyStamp]=@ConcurrencyStamp
WHERE Id=@Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM id.AspNetRoles WHERE Id=@Id;";

        public string FindByIdSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[Name] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
FROM id.AspNetRoles
WHERE Id=@Id;";

        public string FindByNameSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[Name] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
FROM id.AspNetRoles
WHERE Name=@NormalizedName;";
    }
}
