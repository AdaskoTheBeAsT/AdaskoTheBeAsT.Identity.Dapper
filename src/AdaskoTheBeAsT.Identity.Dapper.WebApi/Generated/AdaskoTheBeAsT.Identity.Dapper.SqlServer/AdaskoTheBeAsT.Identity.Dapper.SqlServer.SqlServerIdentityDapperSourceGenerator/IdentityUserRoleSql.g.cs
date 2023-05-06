using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity
{
    public class IdentityUserRoleSql
        : IIdentityUserRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO id.AspNetUserRoles(
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
            @"DELETE FROM id.AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetByUserIdRoleIdSql { get; } =
            @"SELECT [LoginProvider] AS LoginProvider
,[ProviderKey] AS ProviderKey
,[ProviderDisplayName] AS ProviderDisplayName
,[UserId] AS UserId
FROM id.AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetCountSql { get; } =
            @"SELECT COUNT(*)
FROM id.AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetRoleNamesByUserIdSql { get; } =
            @"SELECT r.Name
FROM id.AspNetRoles r
INNER JOIN id.AspNetUserRoles ur ON r.Id=ur.RoleId
WHERE ur.UserId=@UserId;";
    }
}
