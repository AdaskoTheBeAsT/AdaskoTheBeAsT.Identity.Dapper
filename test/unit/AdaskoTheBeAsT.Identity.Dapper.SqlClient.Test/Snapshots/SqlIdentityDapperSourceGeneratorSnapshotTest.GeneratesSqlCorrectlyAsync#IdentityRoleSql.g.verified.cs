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
,[ConcurrencyStamp]
,[IsActive])
VALUES(
@Name
,@NormalizedName
,@ConcurrencyStamp
,@Active);
SELECT SCOPE_IDENTITY();";

        public string UpdateSql { get; } =
            @"UPDATE dbo.AspNetRoles
SET [Name]=@Name
,[NormalizedName]=@NormalizedName
,[ConcurrencyStamp]=@ConcurrencyStamp
,[IsActive]=@Active
WHERE Id=@Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetRoles WHERE Id=@Id;";

        public string FindByIdSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[NormalizedName] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
,[IsActive] AS Active
FROM dbo.AspNetRoles
WHERE Id=@Id;";

        public string FindByNameSql { get; } =
            @"SELECT Id
,[Name] AS Name
,[NormalizedName] AS NormalizedName
,[ConcurrencyStamp] AS ConcurrencyStamp
,[IsActive] AS Active
FROM dbo.AspNetRoles
WHERE NormalizedName=@NormalizedName;";
    }
}
