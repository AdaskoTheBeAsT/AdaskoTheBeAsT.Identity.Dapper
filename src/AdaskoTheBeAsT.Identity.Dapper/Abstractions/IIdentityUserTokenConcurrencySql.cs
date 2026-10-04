namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

/// <summary>
/// SQL for atomically replacing a token whose value still matches OriginalValue.
/// </summary>
public interface IIdentityUserTokenConcurrencySql : IIdentityUserTokenSql
{
    string UpdateSql { get; }
}
