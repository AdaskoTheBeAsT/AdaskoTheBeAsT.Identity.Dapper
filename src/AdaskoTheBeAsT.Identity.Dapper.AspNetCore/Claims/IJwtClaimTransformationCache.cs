using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public interface IJwtClaimTransformationCache
{
    Task<IReadOnlyList<ResolvedClaimRecord>> GetOrCreateAsync(
        string userId,
        Func<CancellationToken, Task<IReadOnlyList<ResolvedClaimRecord>>> factory,
        CancellationToken cancellationToken);

    Task InvalidateUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task InvalidateAllAsync(
        CancellationToken cancellationToken = default);
}
