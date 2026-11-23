using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;

public sealed class DistributedCacheTokenRevocationStore : ITokenRevocationStore
{
    private const string CacheKeyPrefix = "idjwt:rev:";

    private readonly IDistributedCache _cache;
    private readonly TimeProvider _timeProvider;

    public DistributedCacheTokenRevocationStore(
        IDistributedCache cache,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _cache = cache;
        _timeProvider = timeProvider;
    }

    public async Task RevokeAsync(
        TokenRevocationEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var nowUtc = _timeProvider.GetUtcNow();
        if (entry.ExpiresAtUtc <= nowUtc)
        {
            return;
        }

        var cacheKey = ComputeCacheKey(entry.Issuer, entry.SubjectType, entry.SubjectKey);
        var payload = entry.RevokedBeforeUtc.HasValue
            ? entry.RevokedBeforeUtc.Value.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
            : "*";

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = entry.ExpiresAtUtc,
        };

        await _cache.SetStringAsync(cacheKey, payload, cacheOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsRevokedAsync(
        TokenRevocationCheckContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!string.IsNullOrWhiteSpace(context.JwtId) &&
            await CheckSingleKeyAsync(context.Issuer, RevocationSubjectType.Jti, context.JwtId, context.IssuedAtUtc, cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(context.SessionId) &&
            await CheckSingleKeyAsync(context.Issuer, RevocationSubjectType.SessionId, context.SessionId, context.IssuedAtUtc, cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(context.UserId) &&
            await CheckSingleKeyAsync(context.Issuer, RevocationSubjectType.UserId, context.UserId, context.IssuedAtUtc, cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        return false;
    }

    public Task<int> PruneExpiredAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(0);
    }

    internal static string ComputeCacheKey(
        string issuer,
        RevocationSubjectType subjectType,
        string subjectKey)
    {
        var canonical = InMemoryTokenRevocationStore.BuildLookupKey(issuer, subjectType, subjectKey);
        var hashHex = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return string.Concat(CacheKeyPrefix, hashHex);
    }

    private async Task<bool> CheckSingleKeyAsync(
        string issuer,
        RevocationSubjectType subjectType,
        string subjectKey,
        DateTimeOffset? issuedAtUtc,
        CancellationToken cancellationToken)
    {
        var cacheKey = ComputeCacheKey(issuer, subjectType, subjectKey);
        var raw = await _cache.GetStringAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        if (subjectType == RevocationSubjectType.UserId &&
            !string.Equals(raw, "*", StringComparison.Ordinal) &&
            long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cutoffMs) &&
            issuedAtUtc.HasValue)
        {
            var cutoffUtc = DateTimeOffset.FromUnixTimeMilliseconds(cutoffMs);
            return issuedAtUtc.Value <= cutoffUtc;
        }

        return true;
    }
}
