using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace Sample.SqlServer
{
    public class IdentityUserRoleSql
        : IIdentityUserRoleSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO AspNetUserRoles(
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
            @"DELETE FROM AspNetUserRoles
WHERE UserId=@UserId
  AND RoleId=@RoleId;";

        public string GetByUserIdRoleIdSql { get; } =
            @"SELECT [LoginProvider] AS LoginProvider
,[ProviderKey] AS ProviderKey
,[ProviderDisplayName] AS ProviderDisplayName
,[UserId] AS UserId
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
