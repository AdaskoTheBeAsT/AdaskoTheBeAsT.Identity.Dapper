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

public class DapperUserOnlyStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken, TDbConnection>
    : IUserLoginStore<TUser>,
        IUserClaimStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IUserEmailStore<TUser>,
        IUserLockoutStore<TUser>,
        IUserPhoneNumberStore<TUser>,
        IUserTwoFactorStore<TUser>,
        IUserAuthenticationTokenStore<TUser>,
        IUserAuthenticatorKeyStore<TUser>,
        IUserTwoFactorRecoveryCodeStore<TUser>,
        IQueryableUserStore<TUser>
    where TUser : IdentityUser<TKey>
    where TKey : IEquatable<TKey>
    where TUserClaim : IdentityUserClaim<TKey>, new()
    where TUserLogin : IdentityUserLogin<TKey>, new()
    where TUserToken : IdentityUserToken<TKey>, new()
    where TDbConnection : IDbConnection
{
    private const string InternalLoginProvider = "[AspNetUserStore]";
    private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
    private const string RecoveryCodeTokenName = "RecoveryCodes";
    private bool _disposed;

    protected DapperUserOnlyStoreBase(
        IdentityErrorDescriber describer,
        IIdentityDbConnectionProvider<TDbConnection> connectionProvider,
        IIdentityUserSql identityUserSql,
        IIdentityUserClaimSql identityUserClaimSql,
        IIdentityUserLoginSql identityUserLoginSql,
        IIdentityUserTokenSql identityUserTokenSql)
    {
        ErrorDescriber = describer ?? throw new ArgumentNullException(nameof(describer));
        ConnectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        IdentityUserSql = identityUserSql ?? throw new ArgumentNullException(nameof(identityUserSql));
        IdentityUserClaimSql = identityUserClaimSql ?? throw new ArgumentNullException(nameof(identityUserClaimSql));
        IdentityUserLoginSql = identityUserLoginSql ?? throw new ArgumentNullException(nameof(identityUserLoginSql));
        IdentityUserTokenSql = identityUserTokenSql ?? throw new ArgumentNullException(nameof(identityUserTokenSql));
    }

    /// <summary>
    /// Gets or sets the <see cref="T:Microsoft.AspNetCore.Identity.IdentityErrorDescriber" /> for any error that occurred with the current operation.
    /// </summary>
    public IdentityErrorDescriber ErrorDescriber { get; set; }

    public virtual IQueryable<TUser> Users
    {
        get
        {
            using var connection = ConnectionProvider.Provide();
            return connection.Query<TUser>(IdentityUserSql.GetUsersSql).AsQueryable();
        }
    }

    protected IIdentityDbConnectionProvider<TDbConnection> ConnectionProvider { get; }

    protected IIdentityUserSql IdentityUserSql { get; }

    protected IIdentityUserClaimSql IdentityUserClaimSql { get; }

    protected IIdentityUserLoginSql IdentityUserLoginSql { get; }

    protected IIdentityUserTokenSql IdentityUserTokenSql { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the user identifier for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose identifier should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the identifier for the specified <paramref name="user" />.</returns>
    public virtual Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(ConvertIdToString(user.Id) ?? string.Empty);
    }

    /// <summary>
    /// Gets the username for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose name should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the name for the specified <paramref name="user" />.</returns>
    public virtual Task<string?> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.UserName);
    }

    /// <summary>
    /// Sets the given <paramref name="userName" /> for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose name should be set.</param>
    /// <param name="userName">The user name to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.UserName = userName;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the normalized user name for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose normalized name should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the normalized user name for the specified <paramref name="user" />.</returns>
    public virtual Task<string?> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.NormalizedUserName);
    }

    /// <summary>
    /// Sets the given normalized name for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose name should be set.</param>
    /// <param name="normalizedName">The normalized name to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetNormalizedUserNameAsync(TUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates the specified <paramref name="user" /> in the user store.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the <see cref="T:Microsoft.AspNetCore.Identity.IdentityResult" /> of the creation operation.</returns>
    public virtual async Task<IdentityResult> CreateAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        try
        {
            using var connection = ConnectionProvider.Provide();
            await CreateImplAsync(connection, user, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
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
    /// Updates the specified <paramref name="user" /> in the user store.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the <see cref="T:Microsoft.AspNetCore.Identity.IdentityResult" /> of the update operation.</returns>
    public virtual async Task<IdentityResult> UpdateAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        try
        {
            using var connection = ConnectionProvider.Provide();
            await UpdateImplAsync(connection, user, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
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
    /// Deletes the specified <paramref name="user" /> from the user store.
    /// </summary>
    /// <param name="user">The user to delete.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the <see cref="T:Microsoft.AspNetCore.Identity.IdentityResult" /> of the update operation.</returns>
    public virtual async Task<IdentityResult> DeleteAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        try
        {
            using var connection = ConnectionProvider.Provide();
            await DeleteImplAsync(connection, user, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
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
    /// Finds and returns a user, if any, who has the specified <paramref name="userId" />.
    /// </summary>
    /// <param name="userId">The user ID to search for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the user matching the specified <paramref name="userId" /> if it exists.
    /// </returns>
    public virtual async Task<TUser?> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await FindByIdImplAsync(connection, ConvertIdFromString(userId), cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
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
    /// Finds and returns a user, if any, who has the specified normalized user name.
    /// </summary>
    /// <param name="normalizedUserName">The normalized user name to search for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the user matching the specified <paramref name="normalizedUserName" /> if it exists.
    /// </returns>
    public virtual async Task<TUser?> FindByNameAsync(
        string normalizedUserName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await FindByNameImplAsync(connection, normalizedUserName, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Sets the password hash for a user.
    /// </summary>
    /// <param name="user">The user to set the password hash for.</param>
    /// <param name="passwordHash">The password hash to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the password hash for a user.
    /// </summary>
    /// <param name="user">The user to retrieve the password hash for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the password hash for the user.</returns>
    public virtual Task<string?> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.PasswordHash);
    }

    /// <summary>
    /// Returns a flag indicating if the specified user has a password.
    /// </summary>
    /// <param name="user">The user to retrieve the password hash for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> containing a flag indicating if the specified user has a password. If the
    /// user has a password the returned value with be true, otherwise it will be false.</returns>
    public virtual Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.PasswordHash != null);
    }

    /// <summary>
    /// Get the claims associated with the specified <paramref name="user" /> as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user whose claims should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the claims granted to a user.</returns>
    public virtual async Task<IList<Claim>> GetClaimsAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await GetClaimsImplAsync(connection, user.Id, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Adds the <paramref name="claims" /> given to the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to add the claim to.</param>
    /// <param name="claims">The claim to add to the user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual async Task AddClaimsAsync(
        TUser user,
        IEnumerable<Claim> claims,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        await AddClaimsImplAsync(connection, user, claims, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Replaces the <paramref name="claim" /> on the specified <paramref name="user" />, with the <paramref name="newClaim" />.
    /// </summary>
    /// <param name="user">The user to replace the claim on.</param>
    /// <param name="claim">The claim replace.</param>
    /// <param name="newClaim">The new claim replacing the <paramref name="claim" />.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual async Task ReplaceClaimAsync(
        TUser user,
        Claim claim,
        Claim newClaim,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        await ReplaceClaimImplAsync(connection, user, claim, newClaim, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Removes the <paramref name="claims" /> given from the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to remove the claims from.</param>
    /// <param name="claims">The claim to remove.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual async Task RemoveClaimsAsync(
        TUser user,
        IEnumerable<Claim> claims,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        await RemoveClaimsImplAsync(connection, user, claims, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Adds the <paramref name="login" /> given to the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to add the login to.</param>
    /// <param name="login">The login to add to the user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual async Task AddLoginAsync(
        TUser user,
        UserLoginInfo login,
        CancellationToken cancellationToken)
    {
        using var connection = ConnectionProvider.Provide();
        await AddLoginImplAsync(connection, user, login, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Removes the <paramref name="loginProvider" /> given from the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to remove the login from.</param>
    /// <param name="loginProvider">The login to remove from the user.</param>
    /// <param name="providerKey">The key provided by the <paramref name="loginProvider" /> to identify a user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual async Task RemoveLoginAsync(
        TUser user,
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        using var connection = ConnectionProvider.Provide();
        await RemoveLoginImplAsync(connection, user, loginProvider, providerKey, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Retrieves the associated logins for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose associated logins to retrieve.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> for the asynchronous operation, containing a list of <see cref="T:Microsoft.AspNetCore.Identity.UserLoginInfo" /> for the specified <paramref name="user" />, if any.
    /// </returns>
    public virtual async Task<IList<UserLoginInfo>> GetLoginsAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        using var connection = ConnectionProvider.Provide();
        return await GetLoginsImplAsync(connection, user, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Retrieves the user associated with the specified login provider and login provider key..
    /// </summary>
    /// <param name="loginProvider">The login provider who provided the <paramref name="providerKey" />.</param>
    /// <param name="providerKey">The key provided by the <paramref name="loginProvider" /> to identify a user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> for the asynchronous operation, containing the user, if any which matched the specified login provider and key.
    /// </returns>
    public virtual async Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        var val = await FindUserLoginImplAsync(connection, loginProvider, providerKey, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (val != null)
        {
            return await FindUserImplAsync(connection, val.UserId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        return null;
    }

    /// <summary>
    /// Gets a flag indicating whether the email address for the specified <paramref name="user" /> has been verified, true if the email address is verified otherwise
    /// false.
    /// </summary>
    /// <param name="user">The user whose email confirmation status should be returned.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The task object containing the results of the asynchronous operation, a flag indicating whether the email address for the specified <paramref name="user" />
    /// has been confirmed or not.
    /// </returns>
    public virtual Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.EmailConfirmed);
    }

    /// <summary>
    /// Sets the flag indicating whether the specified <paramref name="user" />'s email address has been confirmed or not.
    /// </summary>
    /// <param name="user">The user whose email confirmation status should be set.</param>
    /// <param name="confirmed">A flag indicating if the email address has been confirmed, true if the address is confirmed otherwise false.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the <paramref name="email" /> address for a <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose email should be set.</param>
    /// <param name="email">The email to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual Task SetEmailAsync(TUser user, string? email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.Email = email;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the email address for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose email should be returned.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object containing the results of the asynchronous operation, the email address for the specified <paramref name="user" />.</returns>
    public virtual Task<string?> GetEmailAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.Email);
    }

    /// <summary>
    /// Returns the normalized email for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose email address to retrieve.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The task object containing the results of the asynchronous lookup operation, the normalized email address if any associated with the specified user.
    /// </returns>
    public virtual Task<string?> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.NormalizedEmail);
    }

    /// <summary>
    /// Sets the normalized email for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose email address to set.</param>
    /// <param name="normalizedEmail">The normalized email to set for the specified <paramref name="user" />.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual Task SetNormalizedEmailAsync(TUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the user, if any, associated with the specified, normalized email address.
    /// </summary>
    /// <param name="normalizedEmail">The normalized email address to return the user for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The task object containing the results of the asynchronous lookup operation, the user if any associated with the specified normalized email address.
    /// </returns>
    public virtual async Task<TUser?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await FindByEmailImplAsync(connection, normalizedEmail, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Gets the last <see cref="T:System.DateTimeOffset" /> a user's last lockout expired, if any.
    /// Any time in the past should be indicates a user is not locked out.
    /// </summary>
    /// <param name="user">The user whose lockout date should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the result of the asynchronous query, a <see cref="T:System.DateTimeOffset" /> containing the last time
    /// a user's lockout expired, if any.
    /// </returns>
    public virtual Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.LockoutEnd);
    }

    /// <summary>
    /// Locks out a user until the specified end date has passed. Setting a end date in the past immediately unlocks a user.
    /// </summary>
    /// <param name="user">The user whose lockout date should be set.</param>
    /// <param name="lockoutEnd">The <see cref="T:System.DateTimeOffset" /> after which the <paramref name="user" />'s lockout should end.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Records that failed access has occurred, incrementing the failed access count.
    /// </summary>
    /// <param name="user">The user whose cancellation count should be incremented.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the incremented failed access count.</returns>
    public virtual async Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.AccessFailedCount++;
        await UpdateAsync(user, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        return user.AccessFailedCount;
    }

    /// <summary>
    /// Resets a user's failed access count.
    /// </summary>
    /// <param name="user">The user whose failed access count should be reset.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    /// <remarks>This is typically called after the account is successfully accessed.</remarks>
    public virtual Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves the current failed access count for the specified <paramref name="user" />..
    /// </summary>
    /// <param name="user">The user whose failed access count should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the failed access count.</returns>
    public virtual Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.AccessFailedCount);
    }

    /// <summary>
    /// Retrieves a flag indicating whether user lockout can enabled for the specified user.
    /// </summary>
    /// <param name="user">The user whose ability to be locked out should be returned.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, true if a user can be locked out, otherwise false.
    /// </returns>
    public virtual Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.LockoutEnabled);
    }

    /// <summary>
    /// Set the flag indicating if the specified <paramref name="user" /> can be locked out..
    /// </summary>
    /// <param name="user">The user whose ability to be locked out should be set.</param>
    /// <param name="enabled">A flag indicating if lock out can be enabled for the specified <paramref name="user" />.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the telephone number for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose telephone number should be set.</param>
    /// <param name="phoneNumber">The telephone number to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetPhoneNumberAsync(TUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the telephone number, if any, for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose telephone number should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the user's telephone number, if any.</returns>
    public virtual Task<string?> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.PhoneNumber);
    }

    /// <summary>
    /// Gets a flag indicating whether the specified <paramref name="user" />'s telephone number has been confirmed.
    /// </summary>
    /// <param name="user">The user to return a flag for, indicating whether their telephone number is confirmed.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, returning true if the specified <paramref name="user" /> has a confirmed
    /// telephone number otherwise false.
    /// </returns>
    public virtual Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    /// <summary>
    /// Sets a flag indicating if the specified <paramref name="user" />'s phone number has been confirmed..
    /// </summary>
    /// <param name="user">The user whose telephone number confirmation status should be set.</param>
    /// <param name="confirmed">A flag indicating whether the user's telephone number has been confirmed.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the provided security <paramref name="stamp" /> for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose security stamp should be set.</param>
    /// <param name="stamp">The security stamp to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (stamp == null)
        {
            throw new ArgumentNullException(nameof(stamp));
        }

        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get the security stamp for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose security stamp should be set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the security stamp for the specified <paramref name="user" />.</returns>
    public virtual Task<string?> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.SecurityStamp);
    }

    /// <summary>
    /// Sets a flag indicating whether the specified <paramref name="user" /> has two factor authentication enabled or not,
    /// as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user whose two factor authentication enabled status should be set.</param>
    /// <param name="enabled">A flag indicating whether the specified <paramref name="user" /> has two factor authentication enabled.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a flag indicating whether the specified <paramref name="user" /> has two factor authentication enabled or not,
    /// as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user whose two factor authentication enabled status should be set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing a flag indicating whether the specified
    /// <paramref name="user" /> has two factor authentication enabled or not.
    /// </returns>
    public virtual Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.TwoFactorEnabled);
    }

    /// <summary>
    /// Retrieves all users with the specified claim.
    /// </summary>
    /// <param name="claim">The claim whose users should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> contains a list of users, if any, that contain the specified claim.
    /// </returns>
    public virtual async Task<IList<TUser>> GetUsersForClaimAsync(
        Claim claim,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        return await GetUsersForClaimImplAsync(connection, claim, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Sets the token value for a particular user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="loginProvider">The authentication provider for the token.</param>
    /// <param name="name">The name of the token.</param>
    /// <param name="value">The value of the token.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetTokenAsync(TUser user, string loginProvider, string name, string? value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var connection = ConnectionProvider.Provide();

        return SetTokenImplAsync(connection, user, loginProvider, name, value, cancellationToken);
    }

    /// <summary>
    /// Deletes a token for a user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="loginProvider">The authentication provider for the token.</param>
    /// <param name="name">The name of the token.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var connection = ConnectionProvider.Provide();

        return RemoveTokenImplAsync(connection, user, loginProvider, name, cancellationToken);
    }

    /// <summary>
    /// Returns the token value.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="loginProvider">The authentication provider for the token.</param>
    /// <param name="name">The name of the token.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task<string?> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var connection = ConnectionProvider.Provide();

        return GetTokenImplAsync(connection,user, loginProvider, name, cancellationToken);
    }

    /// <summary>
    /// Sets the authenticator key for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose authenticator key should be set.</param>
    /// <param name="key">The authenticator key to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken)
    {
        return SetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, key, cancellationToken);
    }

    /// <summary>
    /// Get the authenticator key for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose security stamp should be set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the security stamp for the specified <paramref name="user" />.</returns>
    public virtual Task<string?> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken)
    {
        return GetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, cancellationToken);
    }

    /// <summary>
    /// Returns how many recovery code are still valid for a user.
    /// </summary>
    /// <param name="user">The user who owns the recovery code.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The number of valid recovery codes for the user..</returns>
    public virtual Task<int> CountCodesAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return CountCodesImplAsync(user, cancellationToken);
    }

    /// <summary>
    /// Updates the recovery codes for the user while invalidating any previous recovery codes.
    /// </summary>
    /// <param name="user">The user to store new recovery codes for.</param>
    /// <param name="recoveryCodes">The new recovery codes for the user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The new recovery codes for the user.</returns>
    public virtual Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        var value = string.Join(";", recoveryCodes);
        return SetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, value, cancellationToken);
    }

    /// <summary>
    /// Returns whether a recovery code is valid for a user. Note: recovery codes are only valid
    /// once, and will be invalid after use.
    /// </summary>
    /// <param name="user">The user who owns the recovery code.</param>
    /// <param name="code">The recovery code to use.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>True if the recovery code was found for the user.</returns>
    public virtual Task<bool> RedeemCodeAsync(TUser user, string code, CancellationToken cancellationToken)
    {
        var code2 = code;
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (code2 == null)
        {
            throw new ArgumentNullException(nameof(code));
        }

        return RedeemCodeImplAsync(user, code2, cancellationToken);
    }

    protected virtual async Task<int> CountCodesImplAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        var text = (await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) ?? string.Empty;
        return text.Length > 0 ? text.Split(';').Length : 0;
    }

    protected virtual async Task<bool> RedeemCodeImplAsync(
        TUser user,
        string code2,
        CancellationToken cancellationToken)
    {
        var source = ((await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) ?? string.Empty).Split(';');
        if (source.Contains(code2, StringComparer.OrdinalIgnoreCase))
        {
            var recoveryCodes = new List<string>(source.Where(s => !string.Equals(
                s,
                code2,
                StringComparison.Ordinal)));
            await ReplaceCodesAsync(user, recoveryCodes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return true;
        }

        return false;
    }

    protected virtual async Task<string?> GetTokenImplAsync(
        TDbConnection connection,
        TUser user,
        string loginProvider,
        string name,
        CancellationToken cancellationToken)
    {
        return (await FindTokenImplAsync(connection, user, loginProvider, name, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))?.Value;
    }

    protected virtual async Task SetTokenImplAsync(
        TDbConnection connection,
        TUser user,
        string loginProvider,
        string name,
        string? value,
        CancellationToken cancellationToken)
    {
        var val = await FindTokenImplAsync(connection, user, loginProvider, name, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (val == null)
        {
            await AddUserTokenImplAsync(connection, CreateUserToken(user, loginProvider, name, value), cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        else
        {
            val.Value = value;
        }
    }

    protected virtual async Task RemoveTokenImplAsync(
        TDbConnection connection,
        TUser user,
        string loginProvider,
        string name,
        CancellationToken cancellationToken)
    {
        var val = await FindTokenImplAsync(connection, user, loginProvider, name, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (val != null)
        {
            await RemoveUserTokenImplAsync(connection, val, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    /// <summary>
    /// Called to create a new instance of a <see cref="T:Microsoft.AspNetCore.Identity.IdentityUserClaim`1" />.
    /// </summary>
    /// <param name="user">The associated user.</param>
    /// <param name="claim">The associated claim.</param>
    /// <returns></returns>
    protected virtual TUserClaim CreateUserClaim(TUser user, Claim claim)
    {
        var val = new TUserClaim
        {
            UserId = user.Id,
        };
        val.InitializeFromClaim(claim);
        return val;
    }

    /// <summary>
    /// Called to create a new instance of a <see cref="T:Microsoft.AspNetCore.Identity.IdentityUserLogin`1" />.
    /// </summary>
    /// <param name="user">The associated user.</param>
    /// <param name="login">The associated login.</param>
    /// <returns></returns>
    protected virtual TUserLogin CreateUserLogin(TUser user, UserLoginInfo login)
    {
        return new TUserLogin
        {
            UserId = user.Id,
            ProviderKey = login.ProviderKey,
            LoginProvider = login.LoginProvider,
            ProviderDisplayName = login.ProviderDisplayName,
        };
    }

    /// <summary>
    /// Called to create a new instance of a <see cref="T:Microsoft.AspNetCore.Identity.IdentityUserToken`1" />.
    /// </summary>
    /// <param name="user">The associated user.</param>
    /// <param name="loginProvider">The associated login provider.</param>
    /// <param name="name">The name of the user token.</param>
    /// <param name="value">The value of the user token.</param>
    /// <returns></returns>
    protected virtual TUserToken CreateUserToken(TUser user, string loginProvider, string name, string? value)
    {
        return new TUserToken
        {
            UserId = user.Id,
            LoginProvider = loginProvider,
            Name = name,
            Value = value,
        };
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

    protected virtual async Task CreateImplAsync(
        TDbConnection connection,
        TUser user,
        CancellationToken cancellationToken)
    {
        user.Id = await connection.QueryFirstAsync<TKey>(IdentityUserSql.CreateSql, user).ConfigureAwait(continueOnCapturedContext: false);
    }

    protected virtual async Task UpdateImplAsync(
        TDbConnection connection,
        TUser user,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(IdentityUserSql.UpdateSql, user).ConfigureAwait(continueOnCapturedContext: false);

    protected virtual async Task DeleteImplAsync(
        TDbConnection connection,
        TUser user,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(IdentityUserSql.DeleteSql, user).ConfigureAwait(continueOnCapturedContext: false);

    protected virtual async Task<TUser?> FindByIdImplAsync(
        TDbConnection connection,
        TKey? userId,
        CancellationToken cancellationToken) =>
        await connection.QueryFirstOrDefaultAsync<TUser?>(
                IdentityUserSql.FindByIdSql,
                new { Id = userId })
            .ConfigureAwait(continueOnCapturedContext: false);

    protected virtual async Task<TUser?> FindByNameImplAsync(
        TDbConnection connection,
        string normalizedUserName,
        CancellationToken cancellationToken) =>
        await connection.QueryFirstOrDefaultAsync<TUser?>(
                IdentityUserSql.FindByNameSql,
                new { NormalizedUserName = normalizedUserName })
            .ConfigureAwait(continueOnCapturedContext: false);

    protected virtual async Task<IList<Claim>> GetClaimsImplAsync(
        TDbConnection connection,
        TKey userId,
        CancellationToken cancellationToken) =>
        (await connection.QueryAsync<Claim>(
                IdentityUserClaimSql.GetByUserIdSql,
                new { Id = userId })
            .ConfigureAwait(continueOnCapturedContext: false))
        .AsList();

    protected virtual async Task AddClaimsImplAsync(
        TDbConnection connection,
        TUser user,
        IEnumerable<Claim> claims,
        CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            await connection.ExecuteAsync(
                    IdentityUserClaimSql.CreateSql,
                    CreateUserClaim(user, claim))
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    protected virtual async Task ReplaceClaimImplAsync(
        TDbConnection connection,
        TUser user,
        Claim claim,
        Claim newClaim,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(
                IdentityUserClaimSql.ReplaceSql,
                new
                {
                    UserId = user.Id,
                    ClaimTypeOld = claim.Type,
                    ClaimValueOld = claim.Value,
                    ClaimTypeNew = newClaim.Type,
                    ClaimValueNew = newClaim.Value,
                })
            .ConfigureAwait(continueOnCapturedContext: false);

    protected virtual async Task RemoveClaimsImplAsync(
        TDbConnection connection,
        TUser user,
        IEnumerable<Claim> claims,
        CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            await connection.ExecuteAsync(
                    IdentityUserClaimSql.DeleteSql,
                    CreateUserClaim(user, claim))
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    protected virtual async Task AddLoginImplAsync(
        TDbConnection connection,
        TUser user,
        UserLoginInfo login,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(
                IdentityUserLoginSql.CreateSql,
                CreateUserLogin(user, login))
            .ConfigureAwait(continueOnCapturedContext: false);

    protected virtual async Task RemoveLoginImplAsync(
        TDbConnection connection,
        TUser user,
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(
                IdentityUserLoginSql.DeleteSql,
                new
                {
                    LoginProvider = loginProvider,
                    ProviderKey = providerKey,
                    UserId = user.Id,
                })
            .ConfigureAwait(continueOnCapturedContext: false);

    protected virtual async Task<IList<UserLoginInfo>> GetLoginsImplAsync(
        TDbConnection connection,
        TUser user,
        CancellationToken cancellationToken) =>
        (await connection.QueryAsync<UserLoginInfo>(
                IdentityUserLoginSql.GetByUserIdSql,
                user)
            .ConfigureAwait(continueOnCapturedContext: false))
        .AsList();

    /// <summary>
    /// Return a user with the matching userId if it exists.
    /// </summary>
    /// <param name="connection">Connection to db object.</param>
    /// <param name="userId">The user's id.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user if it exists.</returns>
    protected virtual async Task<TUser?> FindUserImplAsync(
        TDbConnection connection,
        TKey userId,
        CancellationToken cancellationToken)
    {
        return await connection.QueryFirstOrDefaultAsync<TUser?>(
                IdentityUserSql.FindByIdSql,
                new { Id = userId })
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Return a user login with the matching userId, provider, providerKey if it exists.
    /// </summary>
    /// <param name="connection">Connection to db object.</param>
    /// <param name="userId">The user's id.</param>
    /// <param name="loginProvider">The login provider name.</param>
    /// <param name="providerKey">The key provided by the <paramref name="loginProvider" /> to identify a user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user login if it exists.</returns>
    protected virtual async Task<TUserLogin?> FindUserLoginImplAsync(
        TDbConnection connection,
        TKey userId,
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        return await connection.QueryFirstOrDefaultAsync<TUserLogin>(
                IdentityUserLoginSql.GetByUserIdLoginProviderKeySql,
                new
                {
                    UserId = userId,
                    LoginProvider = loginProvider,
                    ProviderKey = providerKey,
                })
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Return a user login with  provider, providerKey if it exists.
    /// </summary>
    /// <param name="connection">Connection to db object.</param>
    /// <param name="loginProvider">The login provider name.</param>
    /// <param name="providerKey">The key provided by the <paramref name="loginProvider" /> to identify a user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user login if it exists.</returns>
    protected virtual async Task<TUserLogin?> FindUserLoginImplAsync(
        TDbConnection connection,
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        return await connection.QueryFirstOrDefaultAsync<TUserLogin>(
                IdentityUserLoginSql.GetByLoginProviderKeySql,
                new
                {
                    LoginProvider = loginProvider,
                    ProviderKey = providerKey,
                })
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    protected virtual async Task<TUser?> FindByEmailImplAsync(
        TDbConnection connection,
        string normalizedEmail,
        CancellationToken cancellationToken) =>
        await connection.QueryFirstOrDefaultAsync<TUser?>(
                IdentityUserSql.FindByEmailSql,
                new { NormalizedEmail = normalizedEmail })
            .ConfigureAwait(continueOnCapturedContext: false);

    protected virtual async Task<IList<TUser>> GetUsersForClaimImplAsync(
        TDbConnection connection,
        Claim claim,
        CancellationToken cancellationToken) =>
        (await connection.QueryAsync<TUser>(
                IdentityUserSql.GetUsersForClaimSql,
                new
                {
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value,
                })
            .ConfigureAwait(continueOnCapturedContext: false))
        .AsList();

   

    /// <summary>
    /// Find a user token if it exists.
    /// </summary>
    /// <param name="connection">Connection to db.</param>
    /// <param name="user">The token owner.</param>
    /// <param name="loginProvider">The login provider for the token.</param>
    /// <param name="name">The name of the token.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user token if it exists.</returns>
    protected virtual async Task<TUserToken?> FindTokenImplAsync(
        TDbConnection connection,
        TUser user,
        string loginProvider,
        string name,
        CancellationToken cancellationToken) =>
        await connection.QueryFirstOrDefaultAsync<TUserToken?>(
                IdentityUserTokenSql.GetByUserIdSql,
                CreateUserToken(user, loginProvider, name, value: null))
            .ConfigureAwait(continueOnCapturedContext: false);


    /// <summary>
    /// Add a new user token.
    /// </summary>
    /// <param name="connection">Connection to db.</param>
    /// <param name="token">The token to be added.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected virtual async Task AddUserTokenImplAsync(
        TDbConnection connection,
        TUserToken token,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(
                IdentityUserTokenSql.CreateSql,
                token)
            .ConfigureAwait(continueOnCapturedContext: false);


    /// <summary>
    /// Remove a new user token.
    /// </summary>
    /// <param name="connection">Connection to db.</param>
    /// <param name="token">The token to be removed.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected virtual async Task RemoveUserTokenImplAsync(
        TDbConnection connection,
        TUserToken token,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(
                IdentityUserTokenSql.DeleteSql,
                token)
            .ConfigureAwait(continueOnCapturedContext: false);
}
