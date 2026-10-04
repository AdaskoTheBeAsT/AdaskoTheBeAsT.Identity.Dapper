namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

/// <summary>SQL that compares concurrency stamps on role updates and deletes.</summary>
public interface IIdentityRoleConcurrencySql : IIdentityRoleSql;
