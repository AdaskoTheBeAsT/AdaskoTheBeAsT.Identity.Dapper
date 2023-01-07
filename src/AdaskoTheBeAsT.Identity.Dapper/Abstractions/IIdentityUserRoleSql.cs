namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityUserRoleSql
{
    string CreateSql { get; }

    string DeleteSql { get; }

    string GetByUserIdRoleIdSql { get; }

    string GetCountSql { get; }

    string GetRoleNamesByUserIdSql { get; }
}
