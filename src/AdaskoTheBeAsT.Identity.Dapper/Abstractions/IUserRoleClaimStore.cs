using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

/// <summary>
/// Provides an abstraction for a store of claims for a user and all his roles.
/// </summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
public interface IUserRoleClaimStore<in TUser>
    : IDisposable
    where TUser : class
{
    /// <summary>
    /// Gets a list of <see cref="Claim"/>s to be belonging to the roles of specified <paramref name="user"/> as an asynchronous operation.
    /// </summary>
    /// <param name="user">The role whose claims to retrieve.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous query, a list of <see cref="Claim"/>s.
    /// </returns>
    Task<IList<Claim>> GetRoleClaimsAsync(TUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a list of <see cref="Claim"/>s to be belonging to the specified <paramref name="user"/> and all roles as an asynchronous operation.
    /// </summary>
    /// <param name="user">The role whose claims to retrieve.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous query, a list of <see cref="Claim"/>s.
    /// </returns>
    Task<IList<Claim>> GetUserAndRoleClaimsAsync(TUser user, CancellationToken cancellationToken);
}
