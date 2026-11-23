using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public sealed class DefaultJwtClaimsProvider<TUser> : IJwtClaimsProvider<TUser>
    where TUser : class
{
    private readonly UserManager<TUser> _userManager;
    private readonly RoleManager<IdentityRole>? _unusedRoleManager;
    private readonly IServiceProvider _serviceProvider;

    public DefaultJwtClaimsProvider(
        UserManager<TUser> userManager,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _userManager = userManager;
        _serviceProvider = serviceProvider;
        _unusedRoleManager = null;
    }

    public async Task<IReadOnlyList<Claim>> GetClaimsAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        _ = _unusedRoleManager;

        var claimKindOptions = (_serviceProvider.GetService(typeof(IOptionsMonitor<IdentityJwtOptions>))
            as IOptionsMonitor<IdentityJwtOptions>)?.CurrentValue.ClaimKinds;

        var claims = new List<Claim>();
        await AppendIdentityCoreClaimsAsync(user, claimKindOptions, claims).ConfigureAwait(false);

        var rawClaims = await LoadUserAndRoleClaimsAsync(user, cancellationToken).ConfigureAwait(false);
        if (claimKindOptions?.FilterAccessTokenClaimsByKind == true)
        {
            var catalogStore = _serviceProvider.GetService(typeof(IJwtClaimCatalogStore)) as IJwtClaimCatalogStore
                ?? throw new InvalidOperationException(
                    "IJwtClaimCatalogStore must be registered when ClaimKinds.FilterAccessTokenClaimsByKind is enabled.");
            var filtered = await catalogStore
                .FilterAccessTokenClaimsAsync(rawClaims, claimKindOptions.AccessTokenClaimKinds, cancellationToken)
                .ConfigureAwait(false);
            claims.AddRange(filtered);
            return claims;
        }

        claims.AddRange(rawClaims);
        return claims;
    }

    private async Task AppendIdentityCoreClaimsAsync(
        TUser user,
        IdentityJwtClaimKindOptions? claimKindOptions,
        List<Claim> claims)
    {
        var userName = await _userManager.GetUserNameAsync(user).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(userName))
        {
            claims.Add(new Claim(IdentityJwtClaimTypes.UniqueName, userName));
            claims.Add(new Claim(ClaimTypes.Name, userName));
        }

        if (_userManager.SupportsUserEmail)
        {
            var email = await _userManager.GetEmailAsync(user).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(IdentityJwtClaimTypes.Email, email));
            }
        }

        var includeRoles = claimKindOptions?.IncludeRolesInAccessToken ?? true;
        if (includeRoles && _userManager.SupportsUserRole)
        {
            var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            foreach (var role in roles.Where(role => !string.IsNullOrWhiteSpace(role)))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }
    }

    private async Task<IReadOnlyList<Claim>> LoadUserAndRoleClaimsAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        if (_serviceProvider.GetService(typeof(IUserRoleClaimStore<TUser>)) is IUserRoleClaimStore<TUser> combinedStore)
        {
            var combinedClaims = await combinedStore.GetUserAndRoleClaimsAsync(user, cancellationToken).ConfigureAwait(false);
            return combinedClaims as IReadOnlyList<Claim> ?? new List<Claim>(combinedClaims);
        }

        if (_userManager.SupportsUserClaim)
        {
            var userClaims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false);
            return userClaims as IReadOnlyList<Claim> ?? new List<Claim>(userClaims);
        }

        return Array.Empty<Claim>();
    }
}
