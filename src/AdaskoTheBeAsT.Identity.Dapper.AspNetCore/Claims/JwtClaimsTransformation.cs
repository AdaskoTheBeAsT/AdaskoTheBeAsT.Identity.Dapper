using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public sealed class JwtClaimsTransformation : IClaimsTransformation
{
    private readonly IJwtClaimCatalogStore _catalogStore;
    private readonly IJwtClaimTransformationCache _cache;
    private readonly IOptionsMonitor<IdentityJwtValidationOptions> _optionsMonitor;
    private readonly IServiceProvider _serviceProvider;

    public JwtClaimsTransformation(
        IJwtClaimCatalogStore catalogStore,
        IJwtClaimTransformationCache cache,
        IOptionsMonitor<IdentityJwtValidationOptions> optionsMonitor,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(catalogStore);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _catalogStore = catalogStore;
        _cache = cache;
        _optionsMonitor = optionsMonitor;
        _serviceProvider = serviceProvider;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var claimKindOptions = _optionsMonitor.CurrentValue.ClaimKinds;
        if (!claimKindOptions.EnableClaimsTransformation)
        {
            return principal;
        }

        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true })
        {
            return principal;
        }

        if (principal.HasClaim(c => string.Equals(c.Type, IdentityJwtClaimTypes.ClaimsTransformed, StringComparison.Ordinal)))
        {
            return principal;
        }

        var userId = principal.FindFirstValue(IdentityJwtClaimTypes.Subject)
                     ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return principal;
        }

        var cancellationToken = ResolveRequestCancellationToken();
        var resolvedClaims = await _cache.GetOrCreateAsync(
                userId,
                ct => _catalogStore.GetUserClaimsForTransformationAsync(
                    userId,
                    claimKindOptions.TransformationClaimKinds,
                    ct),
                cancellationToken)
            .ConfigureAwait(false);

        return BuildTransformedPrincipal(principal, resolvedClaims);
    }

    private static ClaimsPrincipal BuildTransformedPrincipal(
        ClaimsPrincipal principal,
        IReadOnlyList<ResolvedClaimRecord> resolvedClaims)
    {
        var clone = principal.Clone();
        if (clone.Identity is not ClaimsIdentity targetIdentity)
        {
            return principal;
        }

        var existing = new HashSet<(string Type, string Value)>();
        foreach (var claim in clone.Claims)
        {
            existing.Add((claim.Type.ToUpperInvariant(), claim.Value));
        }

        foreach (var resolved in resolvedClaims)
        {
            var key = (resolved.ClaimType.ToUpperInvariant(), resolved.ClaimValue);
            if (!existing.Add(key))
            {
                continue;
            }

            var valueType = string.IsNullOrWhiteSpace(resolved.ValueType)
                ? ClaimValueTypes.String
                : resolved.ValueType;
            targetIdentity.AddClaim(new Claim(resolved.ClaimType, resolved.ClaimValue, valueType));
        }

        targetIdentity.AddClaim(new Claim(IdentityJwtClaimTypes.ClaimsTransformed, "true", ClaimValueTypes.Boolean));
        return clone;
    }

    private CancellationToken ResolveRequestCancellationToken()
    {
        var accessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        return accessor?.HttpContext?.RequestAborted ?? CancellationToken.None;
    }
}
