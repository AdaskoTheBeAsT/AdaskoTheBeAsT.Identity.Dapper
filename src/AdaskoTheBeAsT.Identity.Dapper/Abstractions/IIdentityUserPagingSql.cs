namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

/// <summary>SQL that orders users by ID and applies Offset and PageSize in the database.</summary>
public interface IIdentityUserPagingSql : IIdentityUserSql
{
    string GetUsersPageSql { get; }
}
