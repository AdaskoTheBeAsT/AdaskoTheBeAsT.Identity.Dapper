using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal static class RelationalRows
{
    public static string Key(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static Task<List<T>> FindAsync<T>(RelationalSession session, string table, string key) =>
        session.QueryAsync($"SELECT payload FROM {table} WHERE row_key = @p0", r => Deserialize<T>(r.GetString(0)), key);

    public static Task<int> InsertAsync<T>(
        RelationalSession session,
        string table,
        string key,
        T value,
        long expiry,
        string? scope = null,
        string? owner = null,
        int ordinal = 0) =>
        session.ExecuteAsync(
            $"INSERT INTO {table} (row_key, payload, expiry, scope_key, owner_key, ordinal) VALUES (@p0, @p1, @p2, @p3, @p4, @p5)",
            key,
            new JsonPayload(JsonSerializer.Serialize(value)),
            expiry,
            scope,
            owner,
            ordinal);

    public static Task<int> UpdateAsync<T>(RelationalSession session, string table, string key, T value, long expiry) =>
        session.ExecuteAsync(
            $"UPDATE {table} SET payload = @p1, expiry = @p2 WHERE row_key = @p0",
            key,
            new JsonPayload(JsonSerializer.Serialize(value)),
            expiry);

    public static T Deserialize<T>(string value) =>
        JsonSerializer.Deserialize<T>(value) ?? throw new InvalidOperationException("An Identity JWT store record is invalid.");

    public static async Task<int> PruneAsync(RelationalDatabase database, string partition, int batchSize, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (batchSize <= 0)
        {
            return 0;
        }

        var table = database.Table(partition);
        var now = database.Clock.GetUtcNow().UtcTicks;
        return await database.WriteAsync(
            partition,
            async session =>
            {
                var sql = database.Dialect.Take($"SELECT row_key FROM {table} WHERE expiry <= @p0 ORDER BY expiry, row_key");
                var expired = await session.QueryAsync(sql, static row => row.GetString(0), now, batchSize).ConfigureAwait(false);
                if (expired.Count == 0)
                {
                    return 0;
                }

                var removed = 0;
                foreach (var chunk in expired.Chunk(256))
                {
                    var parameters = string.Join(", ", Enumerable.Range(0, chunk.Length)
                        .Select(i => "@p" + i.ToString(CultureInfo.InvariantCulture)));
                    removed += await session.ExecuteAsync(
                        $"DELETE FROM {table} WHERE row_key IN ({parameters})",
                        chunk).ConfigureAwait(false);
                }

                return removed;
            },
            ct).ConfigureAwait(false);
    }
}
