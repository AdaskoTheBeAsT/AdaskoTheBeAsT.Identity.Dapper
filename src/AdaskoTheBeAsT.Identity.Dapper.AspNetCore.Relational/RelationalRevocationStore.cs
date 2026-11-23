using System.Globalization;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal sealed class RelationalRevocationStore(RelationalDatabase database) : ITokenRevocationStore
{
    private const string Partition = "revocations";
    private readonly string _table = database.Table(Partition);

    public Task RevokeAsync(TokenRevocationEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();
        if (entry.ExpiresAtUtc <= database.Clock.GetUtcNow())
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(entry.SubjectKey))
        {
            throw new ArgumentException("A revocation subject key is required.", nameof(entry));
        }

        var key = Key(entry.Issuer, entry.SubjectType, entry.SubjectKey)!;
        return database.WriteAsync(
            Partition,
            async session =>
            {
                var rows = await RelationalRows.FindAsync<TokenRevocationEntry>(session, _table, key).ConfigureAwait(false);
                var previous = rows.SingleOrDefault();
                if (previous == null)
                {
                    return await RelationalRows.InsertAsync(session, _table, key, entry, entry.ExpiresAtUtc.UtcTicks).ConfigureAwait(false);
                }

                var updated = entry with
                {
                    ExpiresAtUtc = previous.ExpiresAtUtc > entry.ExpiresAtUtc ? previous.ExpiresAtUtc : entry.ExpiresAtUtc,
                    RevokedBeforeUtc = entry.RevokedBeforeUtc is not { } incoming || previous.RevokedBeforeUtc > incoming
                        ? previous.RevokedBeforeUtc
                        : incoming,
                };
                return await RelationalRows.UpdateAsync(session, _table, key, updated, updated.ExpiresAtUtc.UtcTicks).ConfigureAwait(false);
            },
            cancellationToken);
    }

    public Task<bool> IsRevokedAsync(TokenRevocationCheckContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var now = database.Clock.GetUtcNow().UtcTicks;
        return database.ReadAsync(
            async session =>
            {
                var sql = $"SELECT payload FROM {_table} WHERE expiry > @p0 AND row_key IN (@p1, @p2, @p3)";
                var rows = await session.QueryAsync(
                    sql,
                    static r => RelationalRows.Deserialize<TokenRevocationEntry>(r.GetString(0)),
                    now,
                    Key(context.Issuer, RevocationSubjectType.Jti, context.JwtId),
                    Key(context.Issuer, RevocationSubjectType.SessionId, context.SessionId),
                    Key(context.Issuer, RevocationSubjectType.UserId, context.UserId)).ConfigureAwait(false);
                return rows.Any(entry => entry.SubjectType != RevocationSubjectType.UserId ||
                    !entry.RevokedBeforeUtc.HasValue || !context.IssuedAtUtc.HasValue ||
                    context.IssuedAtUtc.Value <= entry.RevokedBeforeUtc.Value);
            },
            cancellationToken);
    }

    public Task<int> PruneExpiredAsync(int batchSize, CancellationToken cancellationToken) =>
        RelationalRows.PruneAsync(database, Partition, batchSize, cancellationToken);

    private static string? Key(string issuer, RevocationSubjectType type, string? subject) =>
        string.IsNullOrWhiteSpace(subject)
            ? null
            : RelationalRows.Key(string.Create(CultureInfo.InvariantCulture, $"{issuer.Length}:{issuer}|{(int)type}|{subject}"));
}
