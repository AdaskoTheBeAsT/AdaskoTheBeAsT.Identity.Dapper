using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal sealed class RelationalMfaStore(RelationalDatabase database) : IMfaChallengeStore, IAtomicMfaChallengeCache
{
    private const string Partition = "mfa";
    private readonly string _table = database.Table(Partition);

    private DistributedCacheMfaChallengeStore Adapter => new(this);

    public Task<MfaChallengeRecord?> GetAsync(string tokenHash, CancellationToken cancellationToken) =>
        database.ReadAsync(
            async session =>
            {
                var rows = await RelationalRows.FindAsync<MfaChallengeRecord>(session, _table, RelationalRows.Key(tokenHash)).ConfigureAwait(false);
                return rows.SingleOrDefault();
            },
            cancellationToken);

    public Task<bool> CompareExchangeAsync(
        string tokenHash,
        MfaChallengeRecord? expected,
        MfaChallengeRecord? replacement,
        CancellationToken cancellationToken) =>
        database.WriteAsync(
            Partition,
            async session =>
            {
                var key = RelationalRows.Key(tokenHash);
                var rows = await RelationalRows.FindAsync<MfaChallengeRecord>(session, _table, key).ConfigureAwait(false);
                if (rows.SingleOrDefault() != expected)
                {
                    return false;
                }

                if (replacement == null)
                {
                    await session.ExecuteAsync($"DELETE FROM {_table} WHERE row_key = @p0", key).ConfigureAwait(false);
                }
                else if (expected == null)
                {
                    await RelationalRows.InsertAsync(session, _table, key, replacement, replacement.ExpiresAtUtc.UtcTicks).ConfigureAwait(false);
                }
                else
                {
                    await RelationalRows.UpdateAsync(session, _table, key, replacement, replacement.ExpiresAtUtc.UtcTicks).ConfigureAwait(false);
                }

                return true;
            },
            cancellationToken);

    public Task StoreAsync(MfaChallengeRecord record, CancellationToken cancellationToken) =>
        Adapter.StoreAsync(record, cancellationToken);

    public Task<MfaChallengeRecord?> FindAsync(string tokenHash, DateTimeOffset nowUtc, CancellationToken cancellationToken) =>
        Adapter.FindAsync(tokenHash, nowUtc, cancellationToken);

    public Task<MfaChallengeAttemptResult> IncrementAttemptAsync(string tokenHash, DateTimeOffset nowUtc, CancellationToken cancellationToken) =>
        Adapter.IncrementAttemptAsync(tokenHash, nowUtc, cancellationToken);

    public Task<bool> ConsumeAsync(string tokenHash, DateTimeOffset nowUtc, CancellationToken cancellationToken) =>
        Adapter.ConsumeAsync(tokenHash, nowUtc, cancellationToken);

    public Task<bool> UpdateOtpStateAsync(
        string tokenHash,
        string selectedMethod,
        string otpCodeHash,
        string hashKeyId,
        int resendCount,
        DateTimeOffset resendAvailableAtUtc,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        Adapter.UpdateOtpStateAsync(tokenHash, selectedMethod, otpCodeHash, hashKeyId, resendCount, resendAvailableAtUtc, nowUtc, cancellationToken);

    public Task<bool> TryRecordUsedOtpAsync(
        string replayTokenHash,
        string hashKeyId,
        string userId,
        DateTimeOffset nowUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken) =>
        Adapter.TryRecordUsedOtpAsync(replayTokenHash, hashKeyId, userId, nowUtc, expiresAtUtc, cancellationToken);

    public Task<int> PruneExpiredAsync(int batchSize, CancellationToken cancellationToken) =>
        RelationalRows.PruneAsync(database, Partition, batchSize, cancellationToken);
}
