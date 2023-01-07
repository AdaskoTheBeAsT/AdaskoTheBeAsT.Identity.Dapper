using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace Sample
{
    public class IdentityUserRoleSql
        : IIdentityUserRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO dbo.AspNetUserRoles(
[LoginProvider]
,[ProviderKey]
,[ProviderDisplayName]
,[UserId])
VALUES(
@LoginProvider
,@ProviderKey
,@ProviderDisplayName
,@UserId);";

        public string DeleteSql { get; } =
            @"DELETE FROM dbo.AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetByUserIdRoleIdSql { get; } =
            @"SELECT [LoginProvider] AS LoginProvider
,[ProviderKey] AS ProviderKey
,[ProviderDisplayName] AS ProviderDisplayName
,[UserId] AS UserId
FROM dbo.AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetCountSql { get; } =
            @"SELECT COUNT(*)
FROM dbo.AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetRoleNamesByUserIdSql { get; } =
            @"SELECT r.NormalizedName
FROM dbo.AspNetRoles r
INNER JOIN dbo.AspNetUserRoles ur ON r.Id=ur.RoleId
WHERE ur.UserId=@UserId;";
    }
}
