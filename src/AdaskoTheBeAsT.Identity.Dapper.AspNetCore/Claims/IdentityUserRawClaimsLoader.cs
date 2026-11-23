using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

internal sealed class IdentityUserRawClaimsLoader<TUser> : IJwtUserRawClaimsLoader
    where TUser : class
{
    private readonly IServiceScopeFactory _scopeFactory;

    public IdentityUserRawClaimsLoader(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _scopeFactory = scopeFactory;
    }

    public async Task<IReadOnlyList<Claim>> LoadRawClaimsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<Claim>();
        }

        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var userManager = scope.ServiceProvider.GetService<UserManager<TUser>>();
            if (userManager == null)
            {
                return Array.Empty<Claim>();
            }

            var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                return Array.Empty<Claim>();
            }

            return await LoadClaimsForUserAsync(scope.ServiceProvider, userManager, user, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task<IReadOnlyList<Claim>> LoadClaimsForUserAsync(
        IServiceProvider serviceProvider,
        UserManager<TUser> userManager,
        TUser user,
        CancellationToken cancellationToken)
    {
        if (serviceProvider.GetService<IUserRoleClaimStore<TUser>>() is { } combinedStore)
        {
            var combined = await combinedStore.GetUserAndRoleClaimsAsync(user, cancellationToken).ConfigureAwait(false);
            return combined as IReadOnlyList<Claim> ?? new List<Claim>(combined);
        }

        var claims = new List<Claim>();
        if (userManager.SupportsUserClaim)
        {
            var userClaims = await userManager.GetClaimsAsync(user).ConfigureAwait(false);
            claims.AddRange(userClaims);
        }

        return claims;
    }
}
