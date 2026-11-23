using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Internal;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Stores;

public sealed class SqlServerRefreshTokenStore : IRefreshTokenStore
{
    private const int SqlDeadlockErrorNumber = 1205;
    private const int MaxDeadlockRetries = 6;

    private const string ColumnsList =
        "[Id], [TokenHash], [HashKeyId], [SessionId], [UserId], [SecurityStamp], [AuthenticationMethods], " +
        "[IssuedAtUtc], [ExpiresAtUtc], [SessionExpiresAtUtc], [ConsumedAtUtc], [ReplacedByTokenHash], [RevokedAtUtc], [RevocationReason]";

    private readonly ISqlServerJwtConnectionFactory _connectionFactory;
    private readonly IOptionsMonitor<IdentityJwtSqlServerOptions> _optionsMonitor;
    private readonly TimeProvider _timeProvider;

    public SqlServerRefreshTokenStore(
        ISqlServerJwtConnectionFactory connectionFactory,
        IOptionsMonitor<IdentityJwtSqlServerOptions> optionsMonitor,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _connectionFactory = connectionFactory;
        _optionsMonitor = optionsMonitor;
        _timeProvider = timeProvider;
    }

    public async Task StoreAsync(
        RefreshTokenRecord record,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        var table = GetQualifiedTable();
        var sql = BuildUpsertSql(table);

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            await connection.ExecuteAsync(
                    new CommandDefinition(sql, ToParameters(record), cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }
    }

    public async Task<RefreshTokenRecord?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return null;
        }

        var table = GetQualifiedTable();
        var sql = string.Create(
            CultureInfo.InvariantCulture,
            $"SELECT TOP (1) {ColumnsList} FROM {table} WHERE [TokenHash] = @TokenHash;");

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var row = await connection.QuerySingleOrDefaultAsync<RefreshTokenRow>(
                    new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row?.ToRecord();
        }
    }

    public async Task<RefreshTokenRotationResult> RotateAsync(
        string currentTokenHash,
        RefreshTokenRecord replacement,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        if (string.IsNullOrWhiteSpace(currentTokenHash))
        {
            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.NotFoundOrExpired, null, null);
        }

        var table = GetQualifiedTable();
        for (var attempt = 0; attempt < MaxDeadlockRetries; attempt++)
        {
            try
            {
                return await ExecuteRotationAttemptAsync(
                        table,
                        currentTokenHash,
                        replacement,
                        nowUtc,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (SqlException ex) when (ex.Number == SqlDeadlockErrorNumber)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10 * (attempt + 1)), cancellationToken).ConfigureAwait(false);
            }
        }

        return await ExecuteRotationAttemptAsync(
                table,
                currentTokenHash,
                replacement,
                nowUtc,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> RevokeSessionAsync(
        string sessionId,
        DateTimeOffset nowUtc,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return 0;
        }

        var table = GetQualifiedTable();
        var sql = string.Create(
            CultureInfo.InvariantCulture,
            $"UPDATE {table} SET [RevokedAtUtc] = @NowUtc, [RevocationReason] = @Reason WHERE [SessionId] = @SessionId AND [RevokedAtUtc] IS NULL;");

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            return await connection.ExecuteAsync(
                    new CommandDefinition(
                        sql,
                        new { SessionId = sessionId, NowUtc = nowUtc, Reason = reason },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<string>> RevokeAllUserSessionsAsync(
        string userId,
        DateTimeOffset nowUtc,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<string>();
        }

        var table = GetQualifiedTable();
        var sql = string.Create(
            CultureInfo.InvariantCulture,
            $"UPDATE {table} SET [RevokedAtUtc] = @NowUtc, [RevocationReason] = @Reason OUTPUT INSERTED.[SessionId] WHERE [UserId] = @UserId AND [RevokedAtUtc] IS NULL;");

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var sessionIds = await connection.QueryAsync<string>(
                    new CommandDefinition(
                        sql,
                        new { UserId = userId, NowUtc = nowUtc, Reason = reason },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return sessionIds.Distinct(StringComparer.Ordinal).ToArray();
        }
    }

    public async Task<int> PruneExpiredAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (batchSize <= 0)
        {
            return 0;
        }

        var table = GetQualifiedTable();
        var nowUtc = _timeProvider.GetUtcNow();
        var sql = string.Create(
            CultureInfo.InvariantCulture,
            $"DELETE TOP (@BatchSize) FROM {table} WHERE [ExpiresAtUtc] <= @NowUtc OR [SessionExpiresAtUtc] <= @NowUtc;");

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            return await connection.ExecuteAsync(
                    new CommandDefinition(
                        sql,
                        new { BatchSize = batchSize, NowUtc = nowUtc },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }
    }

    private static async Task<RefreshTokenRotationResult> ExecuteTransactionalRotationAsync(
        SqlConnection connection,
        DbTransaction transaction,
        string table,
        string currentTokenHash,
        RefreshTokenRecord replacement,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var selectSql = string.Create(
            CultureInfo.InvariantCulture,
            $"SELECT TOP (1) {ColumnsList} FROM {table} WITH (XLOCK, ROWLOCK, HOLDLOCK) WHERE [TokenHash] = @TokenHash;");

        var row = await connection.QuerySingleOrDefaultAsync<RefreshTokenRow>(
                new CommandDefinition(
                    selectSql,
                    new { TokenHash = currentTokenHash },
                    transaction: transaction,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (row == null)
        {
            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.NotFoundOrExpired, null, null);
        }

        var current = row.ToRecord();
        if (current.ConsumedAtUtc.HasValue)
        {
            var revokeReplaySql = string.Create(
                CultureInfo.InvariantCulture,
                $"UPDATE {table} WITH (UPDLOCK, ROWLOCK) SET [RevokedAtUtc] = @NowUtc, [RevocationReason] = N'refresh_token_replay' WHERE [SessionId] = @SessionId AND [RevokedAtUtc] IS NULL;");
            await connection.ExecuteAsync(
                    new CommandDefinition(
                        revokeReplaySql,
                        new { current.SessionId, NowUtc = nowUtc },
                        transaction: transaction,
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.Replayed, current, null);
        }

        if (current.RevokedAtUtc.HasValue)
        {
            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.Revoked, current, null);
        }

        if (current.ExpiresAtUtc <= nowUtc || current.SessionExpiresAtUtc <= nowUtc)
        {
            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.NotFoundOrExpired, current, null);
        }

        return await ConsumeAndInsertReplacementAsync(connection, transaction, table, current, replacement, nowUtc, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<RefreshTokenRotationResult> ConsumeAndInsertReplacementAsync(
        SqlConnection connection,
        DbTransaction transaction,
        string table,
        RefreshTokenRecord current,
        RefreshTokenRecord replacement,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var consumeSql = string.Create(
            CultureInfo.InvariantCulture,
            $"UPDATE {table} SET [ConsumedAtUtc] = @NowUtc, [ReplacedByTokenHash] = @ReplacementHash WHERE [TokenHash] = @TokenHash AND [ConsumedAtUtc] IS NULL AND [RevokedAtUtc] IS NULL;");

        var affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    consumeSql,
                    new { current.TokenHash, NowUtc = nowUtc, ReplacementHash = replacement.TokenHash },
                    transaction: transaction,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (affected != 1)
        {
            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.Replayed, current, null);
        }

        var insertSql = BuildInsertSql(table);
        await connection.ExecuteAsync(
                new CommandDefinition(
                    insertSql,
                    ToParameters(replacement),
                    transaction: transaction,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        var consumed = current with
        {
            ConsumedAtUtc = nowUtc,
            ReplacedByTokenHash = replacement.TokenHash,
        };

        return new RefreshTokenRotationResult(RefreshTokenRotationStatus.Rotated, consumed, replacement);
    }

    private static string BuildInsertSql(string table) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"INSERT INTO {table} ({ColumnsList}) VALUES (@Id, @TokenHash, @HashKeyId, @SessionId, @UserId, @SecurityStamp, @AuthenticationMethods, @IssuedAtUtc, @ExpiresAtUtc, @SessionExpiresAtUtc, @ConsumedAtUtc, @ReplacedByTokenHash, @RevokedAtUtc, @RevocationReason);");

    private static string BuildUpsertSql(string table) =>
        $"""
        MERGE {table} WITH (HOLDLOCK) AS target
        USING (SELECT @TokenHash AS [TokenHash]) AS source
        ON target.[TokenHash] = source.[TokenHash]
        WHEN MATCHED THEN
            UPDATE SET
                [Id] = @Id,
                [HashKeyId] = @HashKeyId,
                [SessionId] = @SessionId,
                [UserId] = @UserId,
                [SecurityStamp] = @SecurityStamp,
                [AuthenticationMethods] = @AuthenticationMethods,
                [IssuedAtUtc] = @IssuedAtUtc,
                [ExpiresAtUtc] = @ExpiresAtUtc,
                [SessionExpiresAtUtc] = @SessionExpiresAtUtc,
                [ConsumedAtUtc] = @ConsumedAtUtc,
                [ReplacedByTokenHash] = @ReplacedByTokenHash,
                [RevokedAtUtc] = @RevokedAtUtc,
                [RevocationReason] = @RevocationReason
        WHEN NOT MATCHED THEN
            INSERT ({ColumnsList})
            VALUES (@Id, @TokenHash, @HashKeyId, @SessionId, @UserId, @SecurityStamp, @AuthenticationMethods, @IssuedAtUtc, @ExpiresAtUtc, @SessionExpiresAtUtc, @ConsumedAtUtc, @ReplacedByTokenHash, @RevokedAtUtc, @RevocationReason);
        """;

    private static object ToParameters(RefreshTokenRecord record) =>
        new
        {
            record.Id,
            record.TokenHash,
            record.HashKeyId,
            record.SessionId,
            record.UserId,
            record.SecurityStamp,
            record.AuthenticationMethods,
            record.IssuedAtUtc,
            record.ExpiresAtUtc,
            record.SessionExpiresAtUtc,
            record.ConsumedAtUtc,
            record.ReplacedByTokenHash,
            record.RevokedAtUtc,
            record.RevocationReason,
        };

    private async Task<RefreshTokenRotationResult> ExecuteRotationAttemptAsync(
        string table,
        string currentTokenHash,
        RefreshTokenRecord replacement,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var transaction = await connection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                var result = await ExecuteTransactionalRotationAsync(
                        connection,
                        transaction,
                        table,
                        currentTokenHash,
                        replacement,
                        nowUtc,
                        cancellationToken)
                    .ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
        }
    }

    private string GetQualifiedTable()
    {
        var options = _optionsMonitor.CurrentValue;
        return SqlIdentifierValidator.QuoteQualifiedTable(options.SchemaName, options.RefreshTokensTableName);
    }

    internal sealed class RefreshTokenRow
    {
        public Guid Id { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public string HashKeyId { get; set; } = string.Empty;

        public string SessionId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string? SecurityStamp { get; set; }

        public string AuthenticationMethods { get; set; } = string.Empty;

        public DateTimeOffset IssuedAtUtc { get; set; }

        public DateTimeOffset ExpiresAtUtc { get; set; }

        public DateTimeOffset SessionExpiresAtUtc { get; set; }

        public DateTimeOffset? ConsumedAtUtc { get; set; }

        public string? ReplacedByTokenHash { get; set; }

        public DateTimeOffset? RevokedAtUtc { get; set; }

        public string? RevocationReason { get; set; }

        public RefreshTokenRecord ToRecord() =>
            new(
                Id,
                TokenHash.TrimEnd(),
                HashKeyId,
                SessionId,
                UserId,
                SecurityStamp,
                AuthenticationMethods,
                IssuedAtUtc,
                ExpiresAtUtc,
                SessionExpiresAtUtc,
                ConsumedAtUtc,
                ReplacedByTokenHash?.TrimEnd(),
                RevokedAtUtc,
                RevocationReason);
    }
}
