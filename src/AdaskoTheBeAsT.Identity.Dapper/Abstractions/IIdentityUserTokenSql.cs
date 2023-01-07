namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityUserTokenSql
{
    string CreateSql { get; }

    string DeleteSql { get; }

    string GetByUserIdSql { get; }
}
