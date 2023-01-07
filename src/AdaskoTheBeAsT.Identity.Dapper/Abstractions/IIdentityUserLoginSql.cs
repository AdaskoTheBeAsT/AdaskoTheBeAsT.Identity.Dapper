namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityUserLoginSql
{
    string CreateSql { get; }

    string DeleteSql { get; }

    string GetByUserIdSql { get; }

    string GetByUserIdLoginProviderKeySql { get; }

    string GetByLoginProviderKeySql { get; }
}
