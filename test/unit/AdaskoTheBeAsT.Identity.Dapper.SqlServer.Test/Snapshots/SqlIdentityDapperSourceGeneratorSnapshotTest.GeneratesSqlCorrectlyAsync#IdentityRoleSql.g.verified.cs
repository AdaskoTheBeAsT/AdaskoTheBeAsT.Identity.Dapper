//HintName: IdentityRoleSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityRoleSql
        : IIdentityRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetRoles(
[Name]
,[NormalizedName]
,[ConcurrencyStamp])
OUTPUT inserted.Id
VALUES(
@Name
,@NormalizedName
,@ConcurrencyStamp);";

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
