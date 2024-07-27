namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityUserSql
{
    string CreateSql { get; }

    string UpdateSql { get; }

    string DeleteSql { get; }

    string FindByIdSql { get; }

    string FindByNameSql { get; }

    string FindByEmailSql { get; }

    string GetUsersForClaimSql { get; }

    string GetUsersInRoleSql { get; }

    string GetUsersSql { get; }
}
