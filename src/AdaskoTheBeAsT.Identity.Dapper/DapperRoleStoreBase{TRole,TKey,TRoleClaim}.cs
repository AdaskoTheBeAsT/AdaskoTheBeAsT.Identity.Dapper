using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Dapper;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper;

public class DapperRoleStoreBase<TRole, TKey, TRoleClaim, TDbConnection>
    : IRoleClaimStore<TRole>,
        IQueryableRoleStore<TRole>
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
    where TRoleClaim : IdentityRoleClaim<TKey>, new()
    where TDbConnection : IDbConnection
{
    private bool _disposed;

    /// <summary>
    /// Constructs a new instance of <see cref="T:DapperRoleStore`3" />.
    /// </summary>
    /// <param name="describer">The <see cref="T:Microsoft.AspNetCore.Identity.IdentityErrorDescriber" />.</param>
    /// <param name="connectionProvider">The <see cref="IIdentityDbConnectionProvider{T}"/> instance.</param>
    /// <param name="identityRoleSql">The <see cref="IIdentityRoleSql"/> instance.</param>
    /// <param name="identityRoleClaimSql">The <see cref="IdentityRoleClaimSql"/> instance.</param>
    public DapperRoleStoreBase(
        IdentityErrorDescriber describer,
        IIdentityDbConnectionProvider<TDbConnection> connectionProvider,
        IIdentityRoleSql identityRoleSql,
        IIdentityRoleClaimSql identityRoleClaimSql)
    {
        ErrorDescriber = describer ?? throw new ArgumentNullException(nameof(describer));
        ConnectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        IdentityRoleSql = identityRoleSql ?? throw new ArgumentNullException(nameof(identityRoleSql));
        IdentityRoleClaimSql = identityRoleClaimSql ?? throw new ArgumentNullException(nameof(identityRoleClaimSql));
    }

    /// <summary>
    /// Gets or sets the <see cref="T:Microsoft.AspNetCore.Identity.IdentityErrorDescriber" /> for any error that occurred with the current operation.
    /// </summary>
    public IdentityErrorDescriber ErrorDescriber { get; set; }

    public virtual IQueryable<TRole> Roles
    {
        get
        {
            using var connection = ConnectionProvider.Provide();
            return connection.Query<TRole>(IdentityRoleSql.GetRolesSql).AsQueryable();
        }
    }

    protected IIdentityDbConnectionProvider<TDbConnection> ConnectionProvider { get; }

    protected IIdentityRoleSql IdentityRoleSql { get; }

    protected IIdentityRoleClaimSql IdentityRoleClaimSql { get; }

    /// <summary>
    /// Creates a new role in a store as an asynchronous operation.
    /// </summary>
    /// <param name="role">The role to create in the store.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the <see cref="T:Microsoft.AspNetCore.Identity.IdentityResult" /> of the asynchronous query.</returns>
    public virtual async Task<IdentityResult> CreateAsync(
        TRole role,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        try
        {
            using var connection = ConnectionProvider.Provide();
            await CreateImplAsync(connection, role, cancellationToken).ConfigureAwait(false);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "1",
                    Description = ex.Message,
                });
        }
    }

    /// <summary>
    /// Updates a role in a store as an asynchronous operation.
    /// </summary>
    /// <param name="role">The role to update in the store.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the <see cref="T:Microsoft.AspNetCore.Identity.IdentityResult" /> of the asynchronous query.</returns>
    public virtual async Task<IdentityResult> UpdateAsync(
        TRole role,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        try
        {
            using var connection = ConnectionProvider.Provide();
            await connection.ExecuteAsync(IdentityRoleSql.UpdateSql, role).ConfigureAwait(false);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "2",
                    Description = ex.Message,
                });
        }
    }

    /// <summary>
    /// Deletes a role from the store as an asynchronous operation.
    /// </summary>
    /// <param name="role">The role to delete from the store.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the <see cref="T:Microsoft.AspNetCore.Identity.IdentityResult" /> of the asynchronous query.</returns>
    public virtual async Task<IdentityResult> DeleteAsync(
        TRole role,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        try
        {
            using var connection = ConnectionProvider.Provide();
            await connection.ExecuteAsync(IdentityRoleSql.DeleteSql, role).ConfigureAwait(false);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "3",
                    Description = ex.Message,
                });
        }
    }

    /// <summary>
    /// Gets the ID for a role from the store as an asynchronous operation.
    /// </summary>
    /// <param name="role">The role whose ID should be returned.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the ID of the role.</returns>
    public virtual Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (role == null)
        {
            throw new ArgumentNullException(nameof(role));
        }

        return Task.FromResult(ConvertIdToString(role.Id) ?? string.Empty);
    }

    /// <summary>
    /// Gets the name of a role from the store as an asynchronous operation.
    /// </summary>
    /// <param name="role">The role whose name should be returned.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the name of the role.</returns>
    public virtual Task<string?> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (role == null)
        {
            throw new ArgumentNullException(nameof(role));
        }

        return Task.FromResult(role.Name);
    }

    /// <summary>
    /// Sets the name of a role in the store as an asynchronous operation.
    /// </summary>
    /// <param name="role">The role whose name should be set.</param>
    /// <param name="roleName">The name of the role.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetRoleNameAsync(TRole role, string? roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (role == null)
        {
            throw new ArgumentNullException(nameof(role));
        }

        role.Name = roleName;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Converts the provided <paramref name="id" /> to a strongly typed key object.
    /// </summary>
    /// <param name="id">The id to convert.</param>
    /// <returns>An instance of <typeparamref name="TKey" /> representing the provided <paramref name="id" />.</returns>
    public virtual TKey? ConvertIdFromString(string? id)
    {
        if (id == null)
        {
            return default;
        }

        return (TKey?)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(id);
    }

    /// <summary>
    /// Converts the provided <paramref name="id" /> to its string representation.
    /// </summary>
    /// <param name="id">The id to convert.</param>
    /// <returns>An <see cref="T:System.String" /> representation of the provided <paramref name="id" />.</returns>
    public virtual string? ConvertIdToString(TKey id) => Equals(id, default(TKey)) ? null : id.ToString();

    /// <summary>
    /// Finds the role who has the specified ID as an asynchronous operation.
    /// </summary>
    /// <param name="roleId">The role ID to look for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that result of the look up.</returns>
    public virtual async Task<TRole?> FindByIdAsync(
        string roleId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await connection.QueryFirstOrDefaultAsync<TRole?>(
                IdentityRoleSql.FindByIdSql,
                new { Id = ConvertIdFromString(roleId) })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Finds the role who has the specified normalized name as an asynchronous operation.
    /// </summary>
    /// <param name="normalizedRoleName">The normalized role name to look for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that result of the look up.</returns>
    public virtual async Task<TRole?> FindByNameAsync(
        string normalizedRoleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await connection.QueryFirstOrDefaultAsync<TRole?>(
                IdentityRoleSql.FindByNameSql,
                new { NormalizedName = normalizedRoleName })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get a role's normalized name as an asynchronous operation.
    /// </summary>
    /// <param name="role">The role whose normalized name should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the name of the role.</returns>
    public virtual Task<string?> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (role == null)
        {
            throw new ArgumentNullException(nameof(role));
        }

        return Task.FromResult(role.NormalizedName);
    }

    /// <summary>
    /// Set a role's normalized name as an asynchronous operation.
    /// </summary>
    /// <param name="role">The role whose normalized name should be set.</param>
    /// <param name="normalizedName">The normalized name to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetNormalizedRoleNameAsync(TRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (role == null)
        {
            throw new ArgumentNullException(nameof(role));
        }

        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispose the stores.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Get the claims associated with the specified <paramref name="role" /> as an asynchronous operation.
    /// </summary>
    /// <param name="role">The role whose claims should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the claims granted to a role.</returns>
    public virtual async Task<IList<Claim>> GetClaimsAsync(
        TRole role,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return (await connection.QueryAsync<Claim>(
                    IdentityRoleClaimSql.GetByRoleIdSql,
                    role)
                .ConfigureAwait(false))
            .AsList();
    }

    /// <summary>
    /// Adds the <paramref name="claim" /> given to the specified <paramref name="role" />.
    /// </summary>
    /// <param name="role">The role to add the claim to.</param>
    /// <param name="claim">The claim to add to the role.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual async Task AddClaimAsync(
        TRole role,
        Claim claim,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        await connection.ExecuteAsync(
                IdentityRoleClaimSql.CreateSql,
                CreateRoleClaim(role, claim))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the <paramref name="claim" /> given from the specified <paramref name="role" />.
    /// </summary>
    /// <param name="role">The role to remove the claim from.</param>
    /// <param name="claim">The claim to remove from the role.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual async Task RemoveClaimAsync(
        TRole role,
        Claim claim,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        await connection.ExecuteAsync(
                IdentityRoleClaimSql.DeleteSql,
                CreateRoleClaim(role, claim))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Throws if this class has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Creates a entity representing a role claim.
    /// </summary>
    /// <param name="role">The associated role.</param>
    /// <param name="claim">The associated claim.</param>
    /// <returns>The role claim entity.</returns>
    protected virtual TRoleClaim CreateRoleClaim(TRole role, Claim claim)
    {
        return new TRoleClaim
        {
            RoleId = role.Id,
            ClaimType = claim.Type,
            ClaimValue = claim.Value,
        };
    }

    protected virtual async Task CreateImplAsync(
        TDbConnection connection,
        TRole role,
        CancellationToken cancellationToken)
    {
        role.Id = await connection.QueryFirstAsync<TKey>(IdentityRoleSql.CreateSql, role).ConfigureAwait(false);
    }
}
