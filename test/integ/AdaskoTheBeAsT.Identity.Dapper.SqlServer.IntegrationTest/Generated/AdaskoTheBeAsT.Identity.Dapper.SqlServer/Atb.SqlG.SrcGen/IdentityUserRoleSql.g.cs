using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity
{
    public class IdentityUserRoleSql
        : IIdentityUserRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetUserRoles(
[UserId]
,[RoleId])
VALUES(
@UserId
,@RoleId);";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetByUserIdRoleIdSql { get; } =
            @"SELECT [UserId] AS UserId
,[RoleId] AS RoleId
FROM dbo.AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetCountSql { get; } =
            @"SELECT COUNT(*)
FROM dbo.AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetRoleNamesByUserIdSql { get; } =
            @"SELECT r.Name
FROM dbo.AspNetRoles r
INNER JOIN dbo.AspNetUserRoles ur ON r.Id=ur.RoleId
WHERE ur.UserId=@UserId;";
    }
}
