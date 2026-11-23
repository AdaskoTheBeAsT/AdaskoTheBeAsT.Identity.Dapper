using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

internal sealed class DelegateJwtClaimsProvider<TUser> : IJwtClaimsProvider<TUser>
    where TUser : class
{
    private readonly DefaultJwtClaimsProvider<TUser> _inner;
    private readonly Func<TUser, CancellationToken, Task<IReadOnlyList<Claim>>> _enricher;

    public DelegateJwtClaimsProvider(
        DefaultJwtClaimsProvider<TUser> inner,
        Func<TUser, CancellationToken, Task<IReadOnlyList<Claim>>> enricher)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(enricher);
        _inner = inner;
        _enricher = enricher;
    }

    public async Task<IReadOnlyList<Claim>> GetClaimsAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        var baseClaims = await _inner.GetClaimsAsync(user, cancellationToken).ConfigureAwait(false);
        var extraClaims = await _enricher(user, cancellationToken).ConfigureAwait(false);
        if (extraClaims == null || extraClaims.Count == 0)
        {
            return baseClaims;
        }

        var combined = new List<Claim>(baseClaims.Count + extraClaims.Count);
        combined.AddRange(baseClaims);
        combined.AddRange(extraClaims);
        return combined;
    }
}
