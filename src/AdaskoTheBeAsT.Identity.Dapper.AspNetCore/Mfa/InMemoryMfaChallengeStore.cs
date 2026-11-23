using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public sealed class InMemoryMfaChallengeStore : IMfaChallengeStore
{
    private readonly int _capacity;
    private readonly TimeProvider _timeProvider;
    private readonly System.Threading.Lock _sync = new();
    private readonly Dictionary<string, LinkedListNode<MfaChallengeRecord>> _byHash = new(StringComparer.Ordinal);
    private readonly LinkedList<MfaChallengeRecord> _order = new();

    public InMemoryMfaChallengeStore(
        TimeProvider timeProvider,
        IOptions<IdentityJwtOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
        var configured = options?.Value.Mfa.InMemoryChallengeCapacity ?? 10_000;
        _capacity = configured > 0 ? configured : 10_000;
    }

    public Task StoreAsync(
        MfaChallengeRecord record,
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

    public Task<MfaChallengeRecord?> FindAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Task.FromResult<MfaChallengeRecord?>(null);
        }

        lock (_sync)
        {
            if (!_byHash.TryGetValue(tokenHash, out var node))
            {
                return Task.FromResult<MfaChallengeRecord?>(null);
            }

            if (node.Value.ExpiresAtUtc <= nowUtc)
            {
                _order.Remove(node);
                _byHash.Remove(tokenHash);
                return Task.FromResult<MfaChallengeRecord?>(null);
            }

            return Task.FromResult<MfaChallengeRecord?>(node.Value);
        }
    }

    public Task<MfaChallengeAttemptResult> IncrementAttemptAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Task.FromResult(new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.NotFoundOrExpired, null));
        }

        lock (_sync)
        {
            if (!_byHash.TryGetValue(tokenHash, out var node))
            {
                return Task.FromResult(new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.NotFoundOrExpired, null));
            }

            var current = node.Value;
            if (current.ExpiresAtUtc <= nowUtc)
            {
                _order.Remove(node);
                _byHash.Remove(tokenHash);
                return Task.FromResult(new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.NotFoundOrExpired, null));
            }

            var nextAttempt = current.AttemptCount + 1;
            if (nextAttempt > current.MaxAttempts)
            {
                _order.Remove(node);
                _byHash.Remove(tokenHash);
                return Task.FromResult(new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.Exhausted, current));
            }

            var updated = current with { AttemptCount = nextAttempt };
            node.Value = updated;
            return Task.FromResult(new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.Allowed, updated));
        }
    }

    public Task<bool> ConsumeAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Task.FromResult(false);
        }

        lock (_sync)
        {
            if (!_byHash.TryGetValue(tokenHash, out var node))
            {
                return Task.FromResult(false);
            }

            _order.Remove(node);
            _byHash.Remove(tokenHash);
            return Task.FromResult(node.Value.ExpiresAtUtc > nowUtc);
        }
    }

    public Task<bool> UpdateOtpStateAsync(
        string tokenHash,
        string selectedMethod,
        string otpCodeHash,
        string hashKeyId,
        int resendCount,
        DateTimeOffset resendAvailableAtUtc,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Task.FromResult(false);
        }

        lock (_sync)
        {
            if (!_byHash.TryGetValue(tokenHash, out var node) ||
                node.Value.ExpiresAtUtc <= nowUtc ||
                resendCount <= 0 ||
                node.Value.ResendCount != resendCount - 1 ||
                node.Value.ResendAvailableAtUtc > nowUtc)
            {
                return Task.FromResult(false);
            }

            node.Value = node.Value with
            {
                SelectedMethod = selectedMethod,
                OtpCodeHash = otpCodeHash,
                HashKeyId = hashKeyId,
                ResendCount = resendCount,
                ResendAvailableAtUtc = resendAvailableAtUtc,
            };
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryRecordUsedOtpAsync(
        string replayTokenHash,
        string hashKeyId,
        string userId,
        DateTimeOffset nowUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(replayTokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        lock (_sync)
        {
            if (_byHash.TryGetValue(replayTokenHash, out var existing))
            {
                if (existing.Value.ExpiresAtUtc > nowUtc)
                {
                    return Task.FromResult(false);
                }

                _order.Remove(existing);
                _byHash.Remove(replayTokenHash);
            }

            var guard = new MfaChallengeRecord(
                Guid.NewGuid(),
                replayTokenHash,
                hashKeyId,
                MfaChallengePurpose.OtpReplayGuard,
                userId,
                null,
                string.Empty,
                null,
                null,
                0,
                1,
                0,
                null,
                nowUtc,
                expiresAtUtc);
            StoreInternal(guard);
            return Task.FromResult(true);
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

    private void StoreInternal(MfaChallengeRecord record)
    {
        if (_byHash.TryGetValue(record.TokenHash, out var existing))
        {
            existing.Value = record;
            return;
        }

        if (_byHash.Count >= _capacity && _order.First is { } oldest)
        {
            _order.RemoveFirst();
            _byHash.Remove(oldest.Value.TokenHash);
        }

        var node = _order.AddLast(record);
        _byHash[record.TokenHash] = node;
    }

    private int PruneExpiredInternal(
        DateTimeOffset nowUtc,
        int maxToRemove)
    {
        var removed = 0;
        var current = _order.First;
        while (current != null && removed < maxToRemove)
        {
            var next = current.Next;
            if (current.Value.ExpiresAtUtc <= nowUtc)
            {
                _order.Remove(current);
                _byHash.Remove(current.Value.TokenHash);
                removed++;
            }

            current = next;
        }

        return removed;
    }
}
