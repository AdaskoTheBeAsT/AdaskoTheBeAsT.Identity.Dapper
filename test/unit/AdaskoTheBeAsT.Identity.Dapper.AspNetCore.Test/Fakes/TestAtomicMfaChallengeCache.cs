using System.Collections.Concurrent;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;

internal sealed class TestAtomicMfaChallengeCache : IAtomicMfaChallengeCache
{
    private readonly ConcurrentDictionary<string, MfaChallengeRecord> _records = new(StringComparer.Ordinal);

    public Task<MfaChallengeRecord?> GetAsync(string tokenHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _records.TryGetValue(tokenHash, out var current);
        return Task.FromResult(current);
    }

    public async Task<bool> CompareExchangeAsync(
        string tokenHash,
        MfaChallengeRecord? expected,
        MfaChallengeRecord? replacement,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        if (expected == null)
        {
            return replacement != null && _records.TryAdd(tokenHash, replacement);
        }

        return replacement == null
            ? _records.TryRemove(new KeyValuePair<string, MfaChallengeRecord>(tokenHash, expected))
            : _records.TryUpdate(tokenHash, replacement, expected);
    }
}
