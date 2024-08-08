//HintName: IdentityRoleSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityRoleSql
        : IIdentityRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetRoles(
name
,normalizedname
,concurrencystamp)
VALUES(
@Name
,@NormalizedName
,@ConcurrencyStamp)
RETURNING Id AS ""Id"";";

        public string UpdateSql { get; } =
            @"UPDATE dbo.AspNetRoles
SET name=@Name
,normalizedname=@NormalizedName
,concurrencystamp=@ConcurrencyStamp
WHERE Id=@Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetRoles WHERE Id=@Id;";

        public string FindByIdSql { get; } =
            @"SELECT Id  AS ""Id""
,name AS ""Name""
,normalizedname AS ""NormalizedName""
,concurrencystamp AS ""ConcurrencyStamp""
FROM dbo.AspNetRoles
WHERE Id=@Id;";

        public string FindByNameSql { get; } =
            @"SELECT Id  AS ""Id""
,name AS ""Name""
,normalizedname AS ""NormalizedName""
,concurrencystamp AS ""ConcurrencyStamp""
FROM dbo.AspNetRoles
WHERE NormalizedName=@NormalizedName;";

        public string GetRolesSql { get; } =
            @"SELECT Id  AS ""Id""
,name AS ""Name""
,normalizedname AS ""NormalizedName""
,concurrencystamp AS ""ConcurrencyStamp""
FROM dbo.AspNetRoles;";
    }
}
