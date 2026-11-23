using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

/// <summary>
/// Provides shared, per-key atomic storage for MFA challenges and replay guards.
/// </summary>
public interface IAtomicMfaChallengeCache
{
    /// <summary>
    /// Reads the record for the token hash, or returns null if no record is present.
    /// </summary>
    Task<MfaChallengeRecord?> GetAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically compares the current record with <paramref name="expected"/> by value
    /// and writes <paramref name="replacement"/> only on a match. Null means missing
    /// for the expected record and delete for the replacement. A replacement must expire
    /// at its <see cref="MfaChallengeRecord.ExpiresAtUtc"/>. Use a backend transaction or
    /// native compare-and-exchange, not separate reads and writes or a process-local lock.
    /// Unexpired entries must not be evicted under memory pressure.
    /// </summary>
    Task<bool> CompareExchangeAsync(
        string tokenHash,
        MfaChallengeRecord? expected,
        MfaChallengeRecord? replacement,
        CancellationToken cancellationToken);
}
