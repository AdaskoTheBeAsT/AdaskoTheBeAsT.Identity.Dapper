using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public sealed class DistributedCacheMfaChallengeStore : IMfaChallengeStore
{
    private readonly IAtomicMfaChallengeCache _cache;

    public DistributedCacheMfaChallengeStore(IAtomicMfaChallengeCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    public async Task StoreAsync(
        MfaChallengeRecord record,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!await _cache.CompareExchangeAsync(record.TokenHash, null, record, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("An MFA challenge with the same token hash already exists.");
        }
    }

    public async Task<MfaChallengeRecord?> FindAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return null;
        }

        var record = await _cache.GetAsync(tokenHash, cancellationToken).ConfigureAwait(false);
        return record != null && record.ExpiresAtUtc > nowUtc ? record : null;
    }

    public Task<MfaChallengeAttemptResult> IncrementAttemptAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        ChangeAsync(
            tokenHash,
            nowUtc,
            static record =>
            {
                if (record.AttemptCount >= record.MaxAttempts)
                {
                    return (null, new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.Exhausted, record));
                }

                var updated = record with { AttemptCount = record.AttemptCount + 1 };
                return (updated, new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.Allowed, updated));
            },
            new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.NotFoundOrExpired, null),
            cancellationToken);

    public Task<bool> ConsumeAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        ChangeAsync(tokenHash, nowUtc, static _ => (null, true), false, cancellationToken);

    public Task<bool> UpdateOtpStateAsync(
        string tokenHash,
        string selectedMethod,
        string otpCodeHash,
        string hashKeyId,
        int resendCount,
        DateTimeOffset resendAvailableAtUtc,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        ChangeAsync(
            tokenHash,
            nowUtc,
            record => resendCount <= 0 || record.ResendCount != resendCount - 1 || record.ResendAvailableAtUtc > nowUtc
                ? (record, false)
                : (record with
                {
                    SelectedMethod = selectedMethod,
                    OtpCodeHash = otpCodeHash,
                    HashKeyId = hashKeyId,
                    ResendCount = resendCount,
                    ResendAvailableAtUtc = resendAvailableAtUtc,
                }, true),
            false,
            cancellationToken);

    public async Task<bool> TryRecordUsedOtpAsync(
        string replayTokenHash,
        string hashKeyId,
        string userId,
        DateTimeOffset nowUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayTokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

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
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = await _cache.GetAsync(replayTokenHash, cancellationToken).ConfigureAwait(false);
            if (existing != null && existing.ExpiresAtUtc > nowUtc)
            {
                return false;
            }

            if (await _cache.CompareExchangeAsync(replayTokenHash, existing, guard, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }
    }

    public Task<int> PruneExpiredAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(0);
    }

    private async Task<TResult> ChangeAsync<TResult>(
        string tokenHash,
        DateTimeOffset nowUtc,
        Func<MfaChallengeRecord, (MfaChallengeRecord? Replacement, TResult Result)> change,
        TResult notFoundResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = await FindAsync(tokenHash, nowUtc, cancellationToken).ConfigureAwait(false);
            if (current == null)
            {
                return notFoundResult;
            }

            var (replacement, result) = change(current);
            if (ReferenceEquals(replacement, current) ||
                await _cache.CompareExchangeAsync(tokenHash, current, replacement, cancellationToken).ConfigureAwait(false))
            {
                return result;
            }
        }
    }
}
