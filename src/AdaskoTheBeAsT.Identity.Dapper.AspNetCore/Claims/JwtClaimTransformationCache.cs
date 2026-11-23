using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public sealed class JwtClaimTransformationCache : IJwtClaimTransformationCache
{
    private const string DistributedGenerationKey = "idjwt:claimtx:gen";
    private const string DistributedUserPrefix = "idjwt:claimtx:u:";

    private readonly IMemoryCache _memoryCache;
    private readonly IOptionsMonitor<IdentityJwtValidationOptions> _optionsMonitor;
    private readonly IServiceProvider _serviceProvider;
    private long _memoryGeneration;

    public JwtClaimTransformationCache(
        IMemoryCache memoryCache,
        IOptionsMonitor<IdentityJwtValidationOptions> optionsMonitor,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _memoryCache = memoryCache;
        _optionsMonitor = optionsMonitor;
        _serviceProvider = serviceProvider;
    }

    public Task<IReadOnlyList<ResolvedClaimRecord>> GetOrCreateAsync(
        string userId,
        Func<CancellationToken, Task<IReadOnlyList<ResolvedClaimRecord>>> factory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(factory);

        var claimKindOptions = _optionsMonitor.CurrentValue.ClaimKinds;
        if (claimKindOptions.CacheDuration <= TimeSpan.Zero)
        {
            return InvokeFactoryAsync(factory, cancellationToken);
        }

        var fingerprint = ComputeKindsFingerprint(claimKindOptions.TransformationClaimKinds);
        var distributedCache = ResolveDistributedCache(claimKindOptions);
        if (distributedCache != null)
        {
            return GetOrCreateDistributedAsync(
                distributedCache,
                userId,
                fingerprint,
                claimKindOptions.CacheDuration,
                factory,
                cancellationToken);
        }

        return GetOrCreateMemoryAsync(
            userId,
            fingerprint,
            claimKindOptions.CacheDuration,
            factory,
            cancellationToken);
    }

    public async Task InvalidateUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var memKey = BuildMemoryKey(Volatile.Read(ref _memoryGeneration), userId);
        _memoryCache.Remove(memKey);

        var distributedCache = ResolveDistributedCache(_optionsMonitor.CurrentValue.ClaimKinds);
        if (distributedCache != null)
        {
            var generation = await ReadDistributedGenerationAsync(distributedCache, cancellationToken).ConfigureAwait(false);
            var distKey = BuildDistributedKey(generation, userId);
            await distributedCache.RemoveAsync(distKey, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _memoryGeneration);

        var distributedCache = ResolveDistributedCache(_optionsMonitor.CurrentValue.ClaimKinds);
        if (distributedCache != null)
        {
            await distributedCache.SetStringAsync(
                    DistributedGenerationKey,
                    Guid.NewGuid().ToString("N"),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static string ComputeKindsFingerprint(ISet<string> kindNames)
    {
        if (kindNames.Count == 0)
        {
            return "*";
        }

        return string.Join(
            '|',
            kindNames.Select(k => k.ToUpperInvariant()).OrderBy(k => k, StringComparer.Ordinal));
    }

    private static string BuildMemoryKey(
        long generation,
        string userId) =>
        string.Create(CultureInfo.InvariantCulture, $"idjwt:claimtx:{generation}:{userId}");

    private static string BuildDistributedKey(
        string generation,
        string userId)
    {
        var raw = string.Concat(generation, "|", userId);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return string.Concat(DistributedUserPrefix, hash);
    }

    private static async Task<string> ReadDistributedGenerationAsync(
        IDistributedCache distributedCache,
        CancellationToken cancellationToken)
    {
        var gen = await distributedCache.GetStringAsync(DistributedGenerationKey, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(gen) ? "0" : gen;
    }

    private static Task<IReadOnlyList<ResolvedClaimRecord>> InvokeFactoryAsync(
        Func<CancellationToken, Task<IReadOnlyList<ResolvedClaimRecord>>>? factory,
        CancellationToken cancellationToken) =>
        factory?.Invoke(cancellationToken)
        ?? Task.FromResult<IReadOnlyList<ResolvedClaimRecord>>(Array.Empty<ResolvedClaimRecord>());

    private IDistributedCache? ResolveDistributedCache(IdentityJwtClaimKindOptions options) =>
        options.UseDistributedCache
            ? _serviceProvider.GetService(typeof(IDistributedCache)) as IDistributedCache
            : null;

    private async Task<IReadOnlyList<ResolvedClaimRecord>> GetOrCreateMemoryAsync(
        string userId,
        string fingerprint,
        TimeSpan duration,
        Func<CancellationToken, Task<IReadOnlyList<ResolvedClaimRecord>>> factory,
        CancellationToken cancellationToken)
    {
        var key = BuildMemoryKey(Volatile.Read(ref _memoryGeneration), userId);
        if (_memoryCache.TryGetValue<CachedClaimPayload>(key, out var cached) &&
            cached != null &&
            string.Equals(cached.KindsFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return cached.Claims;
        }

        var loaded = await InvokeFactoryAsync(factory, cancellationToken).ConfigureAwait(false);
        var payload = new CachedClaimPayload(fingerprint, loaded.ToList());
        _memoryCache.Set(key, payload, duration);
        return payload.Claims;
    }

    private async Task<IReadOnlyList<ResolvedClaimRecord>> GetOrCreateDistributedAsync(
        IDistributedCache distributedCache,
        string userId,
        string fingerprint,
        TimeSpan duration,
        Func<CancellationToken, Task<IReadOnlyList<ResolvedClaimRecord>>> factory,
        CancellationToken cancellationToken)
    {
        var generation = await ReadDistributedGenerationAsync(distributedCache, cancellationToken).ConfigureAwait(false);
        var key = BuildDistributedKey(generation, userId);
        var rawJson = await distributedCache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(rawJson))
        {
            var cached = JsonSerializer.Deserialize<CachedClaimPayload>(rawJson);
            if (cached != null && string.Equals(cached.KindsFingerprint, fingerprint, StringComparison.Ordinal))
            {
                return cached.Claims;
            }
        }

        var loaded = await InvokeFactoryAsync(factory, cancellationToken).ConfigureAwait(false);
        var payload = new CachedClaimPayload(fingerprint, loaded.ToList());
        var serialized = JsonSerializer.Serialize(payload);
        await distributedCache.SetStringAsync(
                key,
                serialized,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = duration,
                },
                cancellationToken)
            .ConfigureAwait(false);
        return payload.Claims;
    }

    private sealed record CachedClaimPayload(
        string KindsFingerprint,
        List<ResolvedClaimRecord> Claims);
}
