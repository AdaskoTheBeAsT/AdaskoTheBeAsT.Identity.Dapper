using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal sealed class RelationalRefreshStore(RelationalDatabase database) : IRefreshTokenStore
{
    private const string Partition = "refresh";
    private readonly string _table = database.Table(Partition);

    public Task StoreAsync(RefreshTokenRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        return database.WriteAsync(Partition, session => InsertAsync(session, record), cancellationToken);
    }

    public Task<RefreshTokenRecord?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(tokenHash)
            ? Task.FromResult<RefreshTokenRecord?>(null)
            : database.ReadAsync(session => FindAsync(session, tokenHash), cancellationToken);

    public Task<RefreshTokenRotationResult> RotateAsync(
        string currentTokenHash,
        RefreshTokenRecord replacement,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        if (string.IsNullOrWhiteSpace(currentTokenHash))
        {
            return Task.FromResult(new RefreshTokenRotationResult(RefreshTokenRotationStatus.NotFoundOrExpired, null, null));
        }

        return database.WriteAsync(Partition, session => RotateCoreAsync(session, currentTokenHash, replacement, nowUtc), cancellationToken);
    }

    public Task<int> RevokeSessionAsync(
        string sessionId,
        DateTimeOffset nowUtc,
        string reason,
        CancellationToken cancellationToken) =>
        database.WriteAsync(Partition, session => RevokeAsync(session, "scope_key", RelationalRows.Key(sessionId), nowUtc, reason), cancellationToken);

    public Task<IReadOnlyList<string>> RevokeAllUserSessionsAsync(
        string userId,
        DateTimeOffset nowUtc,
        string reason,
        CancellationToken cancellationToken) =>
        database.WriteAsync<IReadOnlyList<string>>(
            Partition,
            async session =>
            {
                var key = RelationalRows.Key(userId);
                var rows = await session.QueryAsync(
                    $"SELECT payload FROM {_table} WHERE owner_key = @p0 AND revoked IS NULL",
                    r => RelationalRows.Deserialize<RefreshTokenRecord>(r.GetString(0)).SessionId,
                    key).ConfigureAwait(false);
                await RevokeAsync(session, "owner_key", key, nowUtc, reason).ConfigureAwait(false);
                return rows.Distinct(StringComparer.Ordinal).ToArray();
            },
            cancellationToken);

    public Task<int> PruneExpiredAsync(int batchSize, CancellationToken cancellationToken) =>
        RelationalRows.PruneAsync(database, Partition, batchSize, cancellationToken);

    private static long Expiry(RefreshTokenRecord record) =>
        Math.Min(record.ExpiresAtUtc.UtcTicks, record.SessionExpiresAtUtc.UtcTicks);

    private async Task<RefreshTokenRotationResult> RotateCoreAsync(
        RelationalSession session, string currentTokenHash, RefreshTokenRecord replacement, DateTimeOffset now)
    {
        var current = await FindAsync(session, currentTokenHash).ConfigureAwait(false);
        if (current == null)
        {
            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.NotFoundOrExpired, null, null);
        }

        if (current.ConsumedAtUtc.HasValue)
        {
            await RevokeAsync(session, "scope_key", RelationalRows.Key(current.SessionId), now, "refresh_token_replay").ConfigureAwait(false);
            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.Replayed, current, null);
        }

        if (current.RevokedAtUtc.HasValue)
        {
            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.Revoked, current, null);
        }

        if (Expiry(current) <= now.UtcTicks)
        {
            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.NotFoundOrExpired, current, null);
        }

        if (!string.Equals(current.UserId, replacement.UserId, StringComparison.Ordinal) ||
            !string.Equals(current.SessionId, replacement.SessionId, StringComparison.Ordinal) ||
            replacement.SessionExpiresAtUtc != current.SessionExpiresAtUtc)
        {
            throw new ArgumentException("A replacement refresh token must preserve its user, session, and absolute expiry.", nameof(replacement));
        }

        var consumed = current with { ConsumedAtUtc = now, ReplacedByTokenHash = replacement.TokenHash };
        await RelationalRows.UpdateAsync(session, _table, RelationalRows.Key(currentTokenHash), consumed, Expiry(consumed)).ConfigureAwait(false);
        await InsertAsync(session, replacement).ConfigureAwait(false);
        return new RefreshTokenRotationResult(RefreshTokenRotationStatus.Rotated, consumed, replacement);
    }

    private async Task<RefreshTokenRecord?> FindAsync(RelationalSession session, string tokenHash)
    {
        var rows = await session.QueryAsync(
            $"SELECT payload, revoked, reason FROM {_table} WHERE row_key = @p0",
            static reader =>
            {
                var value = RelationalRows.Deserialize<RefreshTokenRecord>(reader.GetString(0));
                if (reader.IsDBNull(1))
                {
                    return value;
                }

                return value with
                {
                    RevokedAtUtc = new DateTimeOffset(reader.GetInt64(1), TimeSpan.Zero),
                    RevocationReason = reader.IsDBNull(2) ? null : reader.GetString(2),
                };
            },
            RelationalRows.Key(tokenHash)).ConfigureAwait(false);
        return rows.SingleOrDefault();
    }

    private async Task<int> InsertAsync(RelationalSession session, RefreshTokenRecord record)
    {
        var key = RelationalRows.Key(record.TokenHash);
        var inserted = await RelationalRows.InsertAsync(
            session,
            _table,
            key,
            record,
            Expiry(record),
            RelationalRows.Key(record.SessionId),
            RelationalRows.Key(record.UserId)).ConfigureAwait(false);
        if (record.RevokedAtUtc is { } revoked)
        {
            await RevokeAsync(session, "row_key", key, revoked, record.RevocationReason).ConfigureAwait(false);
        }

        return inserted;
    }

    private Task<int> RevokeAsync(RelationalSession session, string column, string id, DateTimeOffset now, string? reason) =>
        session.ExecuteAsync(
            $"UPDATE {_table} SET revoked = @p1, reason = @p2 WHERE {column} = @p0 AND revoked IS NULL",
            id,
            now.UtcTicks,
            reason);
}
