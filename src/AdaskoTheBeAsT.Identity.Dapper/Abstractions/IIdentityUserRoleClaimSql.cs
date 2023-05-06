namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityUserRoleClaimSql
{
    string GetRoleClaimsByUserIdSql { get; }

    string GetUserAndRoleClaimsByUserIdSql { get; }
}
