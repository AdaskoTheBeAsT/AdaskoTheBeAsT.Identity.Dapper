using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;

public interface IRefreshTokenStore
{
    Task StoreAsync(
        RefreshTokenRecord record,
        CancellationToken cancellationToken);

    Task<RefreshTokenRecord?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task<RefreshTokenRotationResult> RotateAsync(
        string currentTokenHash,
        RefreshTokenRecord replacement,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task<int> RevokeSessionAsync(
        string sessionId,
        DateTimeOffset nowUtc,
        string reason,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> RevokeAllUserSessionsAsync(
        string userId,
        DateTimeOffset nowUtc,
        string reason,
        CancellationToken cancellationToken);

    Task<int> PruneExpiredAsync(
        int batchSize,
        CancellationToken cancellationToken);
}
