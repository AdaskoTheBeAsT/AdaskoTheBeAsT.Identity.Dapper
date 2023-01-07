namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityUserClaimSql
{
    string CreateSql { get; }

    string DeleteSql { get; }

    string GetByUserIdSql { get; }

    string ReplaceSql { get; }
}
