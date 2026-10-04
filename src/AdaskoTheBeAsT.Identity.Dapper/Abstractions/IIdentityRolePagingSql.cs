namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

/// <summary>SQL that orders roles by ID and applies Offset and PageSize in the database.</summary>
public interface IIdentityRolePagingSql : IIdentityRoleSql
{
    string GetRolesPageSql { get; }
}
