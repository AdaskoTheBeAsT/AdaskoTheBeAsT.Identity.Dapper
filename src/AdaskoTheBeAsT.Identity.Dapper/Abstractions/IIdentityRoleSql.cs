namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityRoleSql
{
    string CreateSql { get; }

    string UpdateSql { get; }

    string DeleteSql { get; }

    string FindByIdSql { get; }

    string FindByNameSql { get; }
}
