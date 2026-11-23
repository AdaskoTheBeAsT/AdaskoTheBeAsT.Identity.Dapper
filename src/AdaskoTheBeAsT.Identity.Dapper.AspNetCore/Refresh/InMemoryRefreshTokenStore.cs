using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;

public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly int _capacity;
    private readonly TimeProvider _timeProvider;
    private readonly System.Threading.Lock _sync = new();
    private readonly Dictionary<string, LinkedListNode<RefreshTokenRecord>> _byHash = new(StringComparer.Ordinal);
    private readonly LinkedList<RefreshTokenRecord> _issuanceOrder = new();

    public InMemoryRefreshTokenStore(
        TimeProvider timeProvider,
        IOptions<IdentityJwtOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
        var configured = options?.Value.InMemoryRefreshTokenCapacity ?? 10_000;
        if (configured <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Refresh token capacity must be positive.");
        }

        _capacity = configured;
    }

    public Task StoreAsync(
        RefreshTokenRecord record,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            PruneExpiredInternal(_timeProvider.GetUtcNow(), maxToRemove: 64);
            StoreInternal(record);
        }

        return Task.CompletedTask;
    }

    public Task<RefreshTokenRecord?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Task.FromResult<RefreshTokenRecord?>(null);
        }

        lock (_sync)
        {
            return Task.FromResult(_byHash.TryGetValue(tokenHash, out var node) ? node.Value : null);
        }
    }

    public Task<RefreshTokenRotationResult> RotateAsync(
        string currentTokenHash,
        RefreshTokenRecord replacement,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(currentTokenHash))
        {
            return Task.FromResult(new RefreshTokenRotationResult(RefreshTokenRotationStatus.NotFoundOrExpired, null, null));
        }

        lock (_sync)
        {
            if (!_byHash.TryGetValue(currentTokenHash, out var existingNode))
            {
                return Task.FromResult(new RefreshTokenRotationResult(RefreshTokenRotationStatus.NotFoundOrExpired, null, null));
            }

            var current = existingNode.Value;
            if (current.ConsumedAtUtc.HasValue)
            {
                RevokeSessionInternal(current.SessionId, nowUtc, "refresh_token_replay");
                return Task.FromResult(new RefreshTokenRotationResult(RefreshTokenRotationStatus.Replayed, current, null));
            }

            if (current.RevokedAtUtc.HasValue)
            {
                return Task.FromResult(new RefreshTokenRotationResult(RefreshTokenRotationStatus.Revoked, current, null));
            }

            if (current.ExpiresAtUtc <= nowUtc || current.SessionExpiresAtUtc <= nowUtc)
            {
                return Task.FromResult(new RefreshTokenRotationResult(RefreshTokenRotationStatus.NotFoundOrExpired, current, null));
            }

            var consumed = current with
            {
                ConsumedAtUtc = nowUtc,
                ReplacedByTokenHash = replacement.TokenHash,
            };
            existingNode.Value = consumed;
            StoreInternal(replacement);
            return Task.FromResult(new RefreshTokenRotationResult(RefreshTokenRotationStatus.Rotated, consumed, replacement));
        }
    }

    public Task<int> RevokeSessionAsync(
        string sessionId,
        DateTimeOffset nowUtc,
        string reason,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.FromResult(0);
        }

        lock (_sync)
        {
            return Task.FromResult(RevokeSessionInternal(sessionId, nowUtc, reason));
        }
    }

    public Task<IReadOnlyList<string>> RevokeAllUserSessionsAsync(
        string userId,
        DateTimeOffset nowUtc,
        string reason,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        lock (_sync)
        {
            var sessions = new HashSet<string>(StringComparer.Ordinal);
            var node = _issuanceOrder.First;
            while (node != null)
            {
                var record = node.Value;
                if (string.Equals(record.UserId, userId, StringComparison.Ordinal) &&
                    !record.RevokedAtUtc.HasValue)
                {
                    node.Value = record with
                    {
                        RevokedAtUtc = nowUtc,
                        RevocationReason = reason,
                    };
                    sessions.Add(record.SessionId);
                }

                node = node.Next;
            }

            return Task.FromResult<IReadOnlyList<string>>(sessions.ToArray());
        }
    }

    public Task<int> PruneExpiredAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (batchSize <= 0)
        {
            return Task.FromResult(0);
        }

        lock (_sync)
        {
            return Task.FromResult(PruneExpiredInternal(_timeProvider.GetUtcNow(), batchSize));
        }
    }

    private void StoreInternal(RefreshTokenRecord record)
    {
        if (_byHash.TryGetValue(record.TokenHash, out var existing))
        {
            existing.Value = record;
            return;
        }

        if (_byHash.Count >= _capacity && _issuanceOrder.First is { } oldest)
        {
            _issuanceOrder.RemoveFirst();
            _byHash.Remove(oldest.Value.TokenHash);
        }

        var node = _issuanceOrder.AddLast(record);
        _byHash[record.TokenHash] = node;
    }

    private int RevokeSessionInternal(
        string sessionId,
        DateTimeOffset nowUtc,
        string reason)
    {
        var count = 0;
        var node = _issuanceOrder.First;
        while (node != null)
        {
            var record = node.Value;
            if (string.Equals(record.SessionId, sessionId, StringComparison.Ordinal) &&
                !record.RevokedAtUtc.HasValue)
            {
                node.Value = record with
                {
                    RevokedAtUtc = nowUtc,
                    RevocationReason = reason,
                };
                count++;
            }

            node = node.Next;
        }

        return count;
    }

    private int PruneExpiredInternal(
        DateTimeOffset nowUtc,
        int maxToRemove)
    {
        var removed = 0;
        var current = _issuanceOrder.First;
        while (current != null && removed < maxToRemove)
        {
            var next = current.Next;
            if (current.Value.ExpiresAtUtc <= nowUtc || current.Value.SessionExpiresAtUtc <= nowUtc)
            {
                _issuanceOrder.Remove(current);
                _byHash.Remove(current.Value.TokenHash);
                removed++;
            }

            current = next;
        }

        return removed;
    }
}
