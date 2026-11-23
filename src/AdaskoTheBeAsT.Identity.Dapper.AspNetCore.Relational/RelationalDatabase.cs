using System.Data;
using System.Data.Common;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal sealed class RelationalDatabase(
    Func<DbConnection> connect,
    RelationalOptions options,
    SqlDialect dialect,
    TimeProvider clock)
{
    public RelationalOptions Options { get; } = options;

    public SqlDialect Dialect { get; } = dialect;

    public TimeProvider Clock { get; } = clock;

    public string Table(string suffix) => Dialect.Table(Options.SchemaName, Options.TablePrefix + "_" + suffix);

    public async Task<T> ReadAsync<T>(Func<RelationalSession, Task<T>> read, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(connect);
        ct.ThrowIfCancellationRequested();
        var connection = connect();
        await using (connection.ConfigureAwait(false))
        {
            await EnsureOpenAsync(connection, ct).ConfigureAwait(false);
            return await read(new RelationalSession(connection, null, Dialect, ct)).ConfigureAwait(false);
        }
    }

    public async Task<T> WriteAsync<T>(string partition, Func<RelationalSession, Task<T>> write, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(connect);
        ct.ThrowIfCancellationRequested();
        var connection = connect();
        await using (connection.ConfigureAwait(false))
        {
            await EnsureOpenAsync(connection, ct).ConfigureAwait(false);
            var isolation = Dialect.IsSqlite ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted;
            var transaction = await connection.BeginTransactionAsync(isolation, ct).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                var session = new RelationalSession(connection, transaction, Dialect, ct);

                // Update a seeded row first. It makes read/modify/write operations safe across processes.
                var locked = await session.ExecuteAsync(
                    $"UPDATE {Table("locks")} SET stamp = 1 - stamp WHERE bucket = @p0", partition).ConfigureAwait(false);
                if (locked != 1)
                {
                    throw new InvalidOperationException("The JWT store lock row is missing. Apply the provider schema first.");
                }

                var result = await write(session).ConfigureAwait(false);
                await transaction.CommitAsync(ct).ConfigureAwait(false);
                return result;
            }
        }
    }

    private static Task EnsureOpenAsync(DbConnection connection, CancellationToken ct) =>
        connection.State == ConnectionState.Open ? Task.CompletedTask : connection.OpenAsync(ct);
}
