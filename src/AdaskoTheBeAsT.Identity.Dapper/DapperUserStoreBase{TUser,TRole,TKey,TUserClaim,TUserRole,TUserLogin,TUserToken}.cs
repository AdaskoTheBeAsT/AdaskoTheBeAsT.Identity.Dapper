using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.Exceptions;
using Dapper;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper;

public class DapperUserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken>
    : DapperUserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>,
        IUserRoleStore<TUser>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
    where TUserClaim : IdentityUserClaim<TKey>, new()
    where TUserRole : IdentityUserRole<TKey>, new()
    where TUserLogin : IdentityUserLogin<TKey>, new()
    where TUserToken : IdentityUserToken<TKey>, new()
{
    public DapperUserStoreBase(
        IdentityErrorDescriber describer,
        IIdentityDbConnectionProvider connectionProvider,
        IIdentityUserSql identityUserSql,
        IIdentityUserClaimSql identityUserClaimSql,
        IIdentityUserLoginSql identityUserLoginSql,
        IIdentityUserTokenSql identityUserTokenSql,
        IIdentityUserRoleSql identityUserRoleSql,
        IIdentityRoleSql identityRoleSql)
        : base(
            describer,
            connectionProvider,
            identityUserSql,
            identityUserClaimSql,
            identityUserLoginSql,
            identityUserTokenSql)
    {
        IdentityUserRoleSql = identityUserRoleSql ?? throw new ArgumentNullException(nameof(identityUserRoleSql));
        IdentityRoleSql = identityRoleSql ?? throw new ArgumentNullException(nameof(identityRoleSql));
    }

    protected IIdentityUserRoleSql IdentityUserRoleSql { get; }

    protected IIdentityRoleSql IdentityRoleSql { get; }

    /// <summary>
    /// Retrieves all users in the specified role.
    /// </summary>
    /// <param name="roleName">The role whose users should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> contains a list of users, if any, that are in the specified role.
    /// </returns>
    public async Task<IList<TUser>> GetUsersInRoleAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return (await connection.QueryAsync<TUser>(
                    IdentityUserSql.GetUsersInRoleSql,
                    new { NormalizedName = roleName })
                .ConfigureAwait(false))
            .AsList();
    }

    /// <summary>
    /// Adds the given <paramref name="roleName" /> to the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to add the role to.</param>
    /// <param name="roleName">The role to add.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public async Task AddToRoleAsync(
        TUser user,
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var role = await FindRoleAsync(roleName, cancellationToken).ConfigureAwait(false);
        if (role == null)
        {
            throw new RoleNotFoundException($"Role {roleName} not found");
        }

        using var connection = ConnectionProvider.Provide();
        await connection.ExecuteAsync(
                IdentityUserRoleSql.CreateSql,
                CreateUserRole(user, role))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the given <paramref name="roleName" /> from the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to remove the role from.</param>
    /// <param name="roleName">The role to remove.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public async Task RemoveFromRoleAsync(
        TUser user,
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var role = await FindRoleAsync(roleName, cancellationToken).ConfigureAwait(false);
        if (role == null)
        {
            throw new RoleNotFoundException($"Role {roleName} not found");
        }

        using var connection = ConnectionProvider.Provide();
        await connection.ExecuteAsync(
                IdentityUserRoleSql.DeleteSql,
                CreateUserRole(user, role))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the roles the specified <paramref name="user" /> is a member of.
    /// </summary>
    /// <param name="user">The user whose roles should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the roles the user is a member of.</returns>
    public async Task<IList<string>> GetRolesAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return (await connection.QueryAsync<string>(
                    IdentityUserRoleSql.GetRoleNamesByUserIdSql,
                    new { UserId = user.Id })
                .ConfigureAwait(false))
            .AsList();
    }

    /// <summary>
    /// Returns a flag indicating if the specified user is a member of the give <paramref name="roleName" />.
    /// </summary>
    /// <param name="user">The user whose role membership should be checked.</param>
    /// <param name="roleName">The role to check membership of.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> containing a flag indicating if the specified user is a member of the given group. If the
    /// user is a member of the group the returned value with be true, otherwise it will be false.</returns>
    public async Task<bool> IsInRoleAsync(
        TUser user,
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var role = await FindRoleAsync(roleName, cancellationToken).ConfigureAwait(false);
        if (role == null)
        {
            throw new RoleNotFoundException($"Role {roleName} not found");
        }

        using var connection = ConnectionProvider.Provide();
        return (await connection.QueryFirstOrDefaultAsync<int>(
                IdentityUserRoleSql.GetCountSql,
                CreateUserRole(user, role))
            .ConfigureAwait(false)) > 0;
    }

    /// <summary>
    /// Called to create a new instance of a <see cref="T:Microsoft.AspNetCore.Identity.IdentityUserRole`1" />.
    /// </summary>
    /// <param name="user">The associated user.</param>
    /// <param name="role">The associated role.</param>
    /// <returns></returns>
    protected virtual TUserRole CreateUserRole(TUser user, TRole role)
    {
        return new TUserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
        };
    }

    /// <summary>
    /// Return a role with the normalized name if it exists.
    /// </summary>
    /// <param name="roleName">The normalized role name.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The role if it exists.</returns>
    protected async Task<TRole?> FindRoleAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await connection.QueryFirstOrDefaultAsync<TRole?>(
                IdentityRoleSql.FindByNameSql,
                new { NormalizedName = roleName })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Return a user role for the userId and roleId if it exists.
    /// </summary>
    /// <param name="userId">The user's id.</param>
    /// <param name="roleId">The role's id.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user role if it exists.</returns>
    protected async Task<TUserRole?> FindUserRoleAsync(
        TKey userId,
        TKey roleId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await connection.QueryFirstOrDefaultAsync<TUserRole?>(
                IdentityUserRoleSql.GetByUserIdRoleIdSql,
                new
                {
                    UserId = userId,
                    RoleId = roleId,
                })
            .ConfigureAwait(false);
    }
}
