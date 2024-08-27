using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.Exceptions;
using Dapper;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper;

public class DapperUserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TDbConnection>
    : DapperUserOnlyStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken, TDbConnection>,
        IUserRoleStore<TUser>,
        IUserRoleClaimStore<TUser>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
    where TUserClaim : IdentityUserClaim<TKey>, new()
    where TUserRole : IdentityUserRole<TKey>, new()
    where TUserLogin : IdentityUserLogin<TKey>, new()
    where TUserToken : IdentityUserToken<TKey>, new()
    where TDbConnection : IDbConnection
{
    public DapperUserStoreBase(
        IdentityErrorDescriber describer,
        IIdentityDbConnectionProvider<TDbConnection> connectionProvider,
        IIdentityUserSql identityUserSql,
        IIdentityUserClaimSql identityUserClaimSql,
        IIdentityUserLoginSql identityUserLoginSql,
        IIdentityUserTokenSql identityUserTokenSql,
        IIdentityUserRoleSql identityUserRoleSql,
        IIdentityRoleSql identityRoleSql,
        IIdentityUserRoleClaimSql identityUserRoleClaimSql)
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
        IdentityUserRoleClaimSql = identityUserRoleClaimSql ?? throw new ArgumentNullException(nameof(identityUserRoleClaimSql));
    }

    protected IIdentityUserRoleSql IdentityUserRoleSql { get; }

    protected IIdentityRoleSql IdentityRoleSql { get; }

    protected IIdentityUserRoleClaimSql IdentityUserRoleClaimSql { get; }

    /// <summary>
    /// Retrieves all users in the specified role.
    /// </summary>
    /// <param name="roleName">The role whose users should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> contains a list of users, if any, that are in the specified role.
    /// </returns>
    public virtual async Task<IList<TUser>> GetUsersInRoleAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await GetUsersInRoleImplAsync(connection, roleName, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Adds the given <paramref name="roleName" /> to the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to add the role to.</param>
    /// <param name="roleName">The role to add.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual async Task AddToRoleAsync(
        TUser user,
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();

        var role = await FindRoleImplAsync(connection, roleName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (role == null)
        {
            throw new RoleNotFoundException($"Role {roleName} not found");
        }

        await AddToRoleImplAsync(connection, user, role, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Removes the given <paramref name="roleName" /> from the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to remove the role from.</param>
    /// <param name="roleName">The role to remove.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual async Task RemoveFromRoleAsync(
        TUser user,
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        var role = await FindRoleImplAsync(connection, roleName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (role == null)
        {
            throw new RoleNotFoundException($"Role {roleName} not found");
        }

        await RemoveFromRoleImplAsync(connection, user, role, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Retrieves the roles the specified <paramref name="user" /> is a member of.
    /// </summary>
    /// <param name="user">The user whose roles should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the roles the user is a member of.</returns>
    public virtual async Task<IList<string>> GetRolesAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await GetRolesImplAsync(connection, user, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Returns a flag indicating if the specified user is a member of the give <paramref name="roleName" />.
    /// </summary>
    /// <param name="user">The user whose role membership should be checked.</param>
    /// <param name="roleName">The role to check membership of.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> containing a flag indicating if the specified user is a member of the given group. If the
    /// user is a member of the group the returned value with be true, otherwise it will be false.</returns>
    public virtual async Task<bool> IsInRoleAsync(
        TUser user,
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        var role = await FindRoleImplAsync(connection, roleName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (role == null)
        {
            throw new RoleNotFoundException($"Role {roleName} not found");
        }

        return await IsInRoleImplAsync(connection, user, role, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Get the claims associated with the roles of the specified <paramref name="user" /> as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user whose claims should be retrieved from his roles.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the claims granted to all roles of given user.</returns>
    public virtual async Task<IList<Claim>> GetRoleClaimsAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await GetRoleClaimsImplAsync(connection, user, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Get the claims associated with the specified <paramref name="user" /> and all his roles as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user whose claims should be retrieved from him and his roles.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the claims granted to a given user and all of his roles.</returns>
    public virtual async Task<IList<Claim>> GetUserAndRoleClaimsAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await GetUserAndRoleClaimsImplAsync(connection, user, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
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

    protected virtual async Task<IList<TUser>> GetUsersInRoleImplAsync(
        TDbConnection connection,
        string roleName,
        CancellationToken cancellationToken) =>
        (await connection.QueryAsync<TUser>(
                IdentityUserSql.GetUsersInRoleSql,
                new { NormalizedName = roleName })
            .ConfigureAwait(continueOnCapturedContext: false))
        .AsList();

    protected virtual async Task AddToRoleImplAsync(
        TDbConnection connection,
        TUser user,
        TRole role,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(
                IdentityUserRoleSql.CreateSql,
                CreateUserRole(user, role))
            .ConfigureAwait(continueOnCapturedContext: false);

    protected virtual async Task RemoveFromRoleImplAsync(
        TDbConnection connection,
        TUser user,
        TRole role,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(
                IdentityUserRoleSql.DeleteSql,
                CreateUserRole(user, role))
            .ConfigureAwait(continueOnCapturedContext: false);

    protected virtual async Task<IList<string>> GetRolesImplAsync(
        TDbConnection connection,
        TUser user,
        CancellationToken cancellationToken) =>
        (await connection.QueryAsync<string>(
                IdentityUserRoleSql.GetRoleNamesByUserIdSql,
                new { UserId = user.Id })
            .ConfigureAwait(continueOnCapturedContext: false))
        .AsList();

    protected virtual async Task<bool> IsInRoleImplAsync(
        TDbConnection connection,
        TUser user,
        TRole role,
        CancellationToken cancellationToken) =>
        (await connection.QueryFirstOrDefaultAsync<int>(
                IdentityUserRoleSql.GetCountSql,
                CreateUserRole(user, role))
            .ConfigureAwait(continueOnCapturedContext: false)) > 0;

    protected virtual async Task<IList<Claim>> GetRoleClaimsImplAsync(
        TDbConnection connection,
        TUser user,
        CancellationToken cancellationToken) =>
        (await connection.QueryAsync<Claim>(
                IdentityUserRoleClaimSql.GetRoleClaimsByUserIdSql,
                user)
            .ConfigureAwait(continueOnCapturedContext: false))
        .AsList();

    protected virtual async Task<IList<Claim>> GetUserAndRoleClaimsImplAsync(
        TDbConnection connection,
        TUser user,
        CancellationToken cancellationToken) =>
        (await connection.QueryAsync<Claim>(
                IdentityUserRoleClaimSql.GetUserAndRoleClaimsByUserIdSql,
                user)
            .ConfigureAwait(continueOnCapturedContext: false))
        .AsList();

    /// <summary>
    /// Return a role with the normalized name if it exists.
    /// </summary>
    /// <param name="connection">Connection to db.</param>
    /// <param name="roleName">The normalized role name.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The role if it exists.</returns>
    protected virtual async Task<TRole?> FindRoleImplAsync(
        TDbConnection connection,
        string roleName,
        CancellationToken cancellationToken) =>
        await connection.QueryFirstOrDefaultAsync<TRole?>(
                IdentityRoleSql.FindByNameSql,
                new { NormalizedName = roleName })
            .ConfigureAwait(continueOnCapturedContext: false);

    /// <summary>
    /// Return a user role for the userId and roleId if it exists.
    /// </summary>
    /// <param name="connection">Connection to db.</param>
    /// <param name="userId">The user's id.</param>
    /// <param name="roleId">The role's id.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user role if it exists.</returns>
    protected virtual async Task<TUserRole?> FindUserRoleAsync(
        TDbConnection connection,
        TKey userId,
        TKey roleId,
        CancellationToken cancellationToken) =>
        await connection.QueryFirstOrDefaultAsync<TUserRole?>(
                IdentityUserRoleSql.GetByUserIdRoleIdSql,
                new
                {
                    UserId = userId,
                    RoleId = roleId,
                })
            .ConfigureAwait(continueOnCapturedContext: false);
}
