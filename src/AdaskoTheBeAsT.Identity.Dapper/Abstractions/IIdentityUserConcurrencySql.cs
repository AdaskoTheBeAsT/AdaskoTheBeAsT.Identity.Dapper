namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

/// <summary>SQL that compares concurrency stamps on user updates and deletes.</summary>
public interface IIdentityUserConcurrencySql : IIdentityUserSql
{
}
