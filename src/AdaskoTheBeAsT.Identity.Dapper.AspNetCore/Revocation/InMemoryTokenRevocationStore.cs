using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;

public sealed class InMemoryTokenRevocationStore : ITokenRevocationStore
{
    private readonly int _capacity;
    private readonly TimeProvider _timeProvider;
    private readonly System.Threading.Lock _sync = new();
    private readonly Dictionary<string, LinkedListNode<StoredNode>> _entries = new(StringComparer.Ordinal);
    private readonly LinkedList<StoredNode> _insertionOrder = new();

    public InMemoryTokenRevocationStore(
        TimeProvider timeProvider,
        IOptions<IdentityJwtOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
        var configured = options?.Value.InMemoryRevocationCapacity ?? 50_000;
        _capacity = configured > 0 ? configured : 50_000;
    }

    public Task RevokeAsync(
        TokenRevocationEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        var nowUtc = _timeProvider.GetUtcNow();
        if (entry.ExpiresAtUtc <= nowUtc)
        {
            return Task.CompletedTask;
        }

        var key = BuildLookupKey(entry.Issuer, entry.SubjectType, entry.SubjectKey);
        lock (_sync)
        {
            PruneExpiredInternal(nowUtc, maxToRemove: 64);

            if (_entries.TryGetValue(key, out var existing))
            {
                var merged = MergeEntry(existing.Value.Entry, entry);
                _insertionOrder.Remove(existing);
                var updatedNode = _insertionOrder.AddLast(new StoredNode(key, merged));
                _entries[key] = updatedNode;
                return Task.CompletedTask;
            }

            if (_entries.Count >= _capacity && _insertionOrder.First is { } oldest)
            {
                _insertionOrder.RemoveFirst();
                _entries.Remove(oldest.Value.Key);
            }

            var node = _insertionOrder.AddLast(new StoredNode(key, entry));
            _entries[key] = node;
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsRevokedAsync(
        TokenRevocationCheckContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var nowUtc = _timeProvider.GetUtcNow();
        lock (_sync)
        {
            if (!string.IsNullOrWhiteSpace(context.JwtId) &&
                TryMatchEntry(context.Issuer, RevocationSubjectType.Jti, context.JwtId, nowUtc, context.IssuedAtUtc))
            {
                return Task.FromResult(true);
            }

            if (!string.IsNullOrWhiteSpace(context.SessionId) &&
                TryMatchEntry(context.Issuer, RevocationSubjectType.SessionId, context.SessionId, nowUtc, context.IssuedAtUtc))
            {
                return Task.FromResult(true);
            }

            if (!string.IsNullOrWhiteSpace(context.UserId) &&
                TryMatchEntry(context.Issuer, RevocationSubjectType.UserId, context.UserId, nowUtc, context.IssuedAtUtc))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
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

        var nowUtc = _timeProvider.GetUtcNow();
        lock (_sync)
        {
            return Task.FromResult(PruneExpiredInternal(nowUtc, batchSize));
        }
    }

    internal static string BuildLookupKey(
        string issuer,
        RevocationSubjectType subjectType,
        string subjectKey) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{issuer.Length}:{issuer}|{(byte)subjectType}|{subjectKey.Length}:{subjectKey}");

    private static TokenRevocationEntry MergeEntry(
        TokenRevocationEntry current,
        TokenRevocationEntry incoming)
    {
        var maxExpires = incoming.ExpiresAtUtc > current.ExpiresAtUtc ? incoming.ExpiresAtUtc : current.ExpiresAtUtc;
        var maxRevokedBefore = incoming.RevokedBeforeUtc;
        if (current.RevokedBeforeUtc.HasValue &&
            (!maxRevokedBefore.HasValue || current.RevokedBeforeUtc.Value > maxRevokedBefore.Value))
        {
            maxRevokedBefore = current.RevokedBeforeUtc;
        }

        return incoming with
        {
            ExpiresAtUtc = maxExpires,
            RevokedBeforeUtc = maxRevokedBefore,
        };
    }

    private bool TryMatchEntry(
        string issuer,
        RevocationSubjectType subjectType,
        string subjectKey,
        DateTimeOffset nowUtc,
        DateTimeOffset? issuedAtUtc)
    {
        var key = BuildLookupKey(issuer, subjectType, subjectKey);
        if (!_entries.TryGetValue(key, out var node))
        {
            return false;
        }

        var entry = node.Value.Entry;
        if (entry.ExpiresAtUtc <= nowUtc)
        {
            _insertionOrder.Remove(node);
            _entries.Remove(key);
            return false;
        }

        if (subjectType == RevocationSubjectType.UserId && entry.RevokedBeforeUtc.HasValue && issuedAtUtc.HasValue)
        {
            return issuedAtUtc.Value <= entry.RevokedBeforeUtc.Value;
        }

        return true;
    }

    private int PruneExpiredInternal(
        DateTimeOffset nowUtc,
        int maxToRemove)
    {
        var removed = 0;
        var current = _insertionOrder.First;
        while (current != null && removed < maxToRemove)
        {
            var next = current.Next;
            if (current.Value.Entry.ExpiresAtUtc <= nowUtc)
            {
                _insertionOrder.Remove(current);
                _entries.Remove(current.Value.Key);
                removed++;
            }

            current = next;
        }

        return removed;
    }

    private sealed record StoredNode(
        string Key,
        TokenRevocationEntry Entry);
}
