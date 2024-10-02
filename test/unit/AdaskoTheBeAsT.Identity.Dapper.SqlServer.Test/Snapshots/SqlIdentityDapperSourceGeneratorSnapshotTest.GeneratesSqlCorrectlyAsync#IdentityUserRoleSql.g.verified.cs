//HintName: IdentityUserRoleSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserRoleSql
        : IIdentityUserRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO AspNetUserRoles(
[UserId]
,[RoleId])
VALUES(
@UserId
,@RoleId);";

        public string DeleteSql { get; } =
            @"DELETE FROM AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetByUserIdRoleIdSql { get; } =
            @"SELECT [UserId] AS UserId
,[RoleId] AS RoleId
FROM AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetCountSql { get; } =
            @"SELECT COUNT(*)
FROM AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetRoleNamesByUserIdSql { get; } =
            @"SELECT r.NormalizedName
FROM AspNetRoles r
INNER JOIN AspNetUserRoles ur ON r.Id=ur.RoleId
WHERE ur.UserId=@UserId;";
    }
}
