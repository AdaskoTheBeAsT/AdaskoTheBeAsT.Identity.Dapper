namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityRoleClaimSql
{
    string CreateSql { get; }

    string DeleteSql { get; }

    string GetByRoleIdSql { get; }
}
