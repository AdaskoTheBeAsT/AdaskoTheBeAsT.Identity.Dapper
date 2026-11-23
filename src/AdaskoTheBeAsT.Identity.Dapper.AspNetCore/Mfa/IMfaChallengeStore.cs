using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public interface IMfaChallengeStore
{
    Task StoreAsync(
        MfaChallengeRecord record,
        CancellationToken cancellationToken);

    Task<MfaChallengeRecord?> FindAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task<MfaChallengeAttemptResult> IncrementAttemptAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task<bool> ConsumeAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Rotates the OTP atomically only if the challenge is unexpired, the cooldown
    /// has ended, and <paramref name="resendCount"/> is exactly one above the stored count.
    /// </summary>
    Task<bool> UpdateOtpStateAsync(
        string tokenHash,
        string selectedMethod,
        string otpCodeHash,
        string hashKeyId,
        int resendCount,
        DateTimeOffset resendAvailableAtUtc,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task<bool> TryRecordUsedOtpAsync(
        string replayTokenHash,
        string hashKeyId,
        string userId,
        DateTimeOffset nowUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken);

    Task<int> PruneExpiredAsync(
        int batchSize,
        CancellationToken cancellationToken);
}
