using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Internal;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Stores;

public sealed class SqlServerMfaChallengeStore : IMfaChallengeStore
{
    private const string ColumnsList =
        "[ChallengeId], [TokenHash], [HashKeyId], [Purpose], [UserId], [SecurityStamp], [AllowedMethods], " +
        "[SelectedMethod], [OtpCodeHash], [AttemptCount], [MaxAttempts], [ResendCount], [ResendAvailableAtUtc], [CreatedAtUtc], [ExpiresAtUtc]";

    private const string InsertedColumnsList =
        "INSERTED.[ChallengeId], INSERTED.[TokenHash], INSERTED.[HashKeyId], INSERTED.[Purpose], INSERTED.[UserId], " +
        "INSERTED.[SecurityStamp], INSERTED.[AllowedMethods], INSERTED.[SelectedMethod], INSERTED.[OtpCodeHash], " +
        "INSERTED.[AttemptCount], INSERTED.[MaxAttempts], INSERTED.[ResendCount], INSERTED.[ResendAvailableAtUtc], " +
        "INSERTED.[CreatedAtUtc], INSERTED.[ExpiresAtUtc]";

    private const string DeletedColumnsList =
        "DELETED.[ChallengeId], DELETED.[TokenHash], DELETED.[HashKeyId], DELETED.[Purpose], DELETED.[UserId], " +
        "DELETED.[SecurityStamp], DELETED.[AllowedMethods], DELETED.[SelectedMethod], DELETED.[OtpCodeHash], " +
        "DELETED.[AttemptCount], DELETED.[MaxAttempts], DELETED.[ResendCount], DELETED.[ResendAvailableAtUtc], " +
        "DELETED.[CreatedAtUtc], DELETED.[ExpiresAtUtc]";

    private readonly ISqlServerJwtConnectionFactory _connectionFactory;
    private readonly IOptionsMonitor<IdentityJwtSqlServerOptions> _optionsMonitor;
    private readonly TimeProvider _timeProvider;

    public SqlServerMfaChallengeStore(
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
        MfaChallengeRecord record,
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

    public async Task<MfaChallengeRecord?> FindAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return null;
        }

        var table = GetQualifiedTable();
        var sql = string.Create(
            CultureInfo.InvariantCulture,
            $"SELECT TOP (1) {ColumnsList} FROM {table} WHERE [TokenHash] = @TokenHash AND [ExpiresAtUtc] > @NowUtc;");

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var row = await connection.QuerySingleOrDefaultAsync<MfaChallengeRow>(
                    new CommandDefinition(
                        sql,
                        new { TokenHash = tokenHash, NowUtc = nowUtc },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return row?.ToRecord();
        }
    }

    public async Task<MfaChallengeAttemptResult> IncrementAttemptAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.NotFoundOrExpired, null);
        }

        var table = GetQualifiedTable();
        var updateSql = string.Create(
            CultureInfo.InvariantCulture,
            $"UPDATE {table} SET [AttemptCount] = [AttemptCount] + 1 OUTPUT {InsertedColumnsList} WHERE [TokenHash] = @TokenHash AND [ExpiresAtUtc] > @NowUtc AND [AttemptCount] < [MaxAttempts];");

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var updatedRow = await connection.QuerySingleOrDefaultAsync<MfaChallengeRow>(
                    new CommandDefinition(
                        updateSql,
                        new { TokenHash = tokenHash, NowUtc = nowUtc },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (updatedRow != null)
            {
                return new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.Allowed, updatedRow.ToRecord());
            }

            return await EvictExhaustedOrExpiredChallengeAsync(connection, table, tokenHash, nowUtc, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task<bool> ConsumeAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return false;
        }

        var table = GetQualifiedTable();
        var sql = string.Create(
            CultureInfo.InvariantCulture,
            $"DELETE FROM {table} OUTPUT DELETED.[ExpiresAtUtc] WHERE [TokenHash] = @TokenHash;");

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var deletedExpiry = await connection.QuerySingleOrDefaultAsync<DateTimeOffset?>(
                    new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return deletedExpiry.HasValue && deletedExpiry.Value > nowUtc;
        }
    }

    public async Task<bool> UpdateOtpStateAsync(
        string tokenHash,
        string selectedMethod,
        string otpCodeHash,
        string hashKeyId,
        int resendCount,
        DateTimeOffset resendAvailableAtUtc,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenHash) || resendCount <= 0)
        {
            return false;
        }

        var table = GetQualifiedTable();
        var sql = BuildUpdateOtpStateSql(table);

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var affected = await connection.ExecuteAsync(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            TokenHash = tokenHash,
                            SelectedMethod = selectedMethod,
                            OtpCodeHash = otpCodeHash,
                            HashKeyId = hashKeyId,
                            ResendCount = resendCount,
                            ResendAvailableAtUtc = resendAvailableAtUtc,
                            NowUtc = nowUtc,
                        },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return affected == 1;
        }
    }

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

        var table = GetQualifiedTable();
        var deleteExpiredSql = string.Create(
            CultureInfo.InvariantCulture,
            $"DELETE FROM {table} WHERE [TokenHash] = @TokenHash AND [ExpiresAtUtc] <= @NowUtc;");

        var insertSql = string.Create(
            CultureInfo.InvariantCulture,
            $"INSERT INTO {table} ({ColumnsList}) VALUES (@ChallengeId, @TokenHash, @HashKeyId, @Purpose, @UserId, NULL, N'', NULL, NULL, 0, 1, 0, NULL, @CreatedAtUtc, @ExpiresAtUtc);");

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            await connection.ExecuteAsync(
                    new CommandDefinition(
                        deleteExpiredSql,
                        new { TokenHash = replayTokenHash, NowUtc = nowUtc },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            try
            {
                await connection.ExecuteAsync(
                        new CommandDefinition(
                            insertSql,
                            new
                            {
                                ChallengeId = Guid.NewGuid(),
                                TokenHash = replayTokenHash,
                                HashKeyId = hashKeyId,
                                Purpose = (byte)MfaChallengePurpose.OtpReplayGuard,
                                UserId = userId,
                                CreatedAtUtc = nowUtc,
                                ExpiresAtUtc = expiresAtUtc,
                            },
                            cancellationToken: cancellationToken))
                    .ConfigureAwait(false);
                return true;
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                return false;
            }
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
            $"DELETE TOP (@BatchSize) FROM {table} WHERE [ExpiresAtUtc] <= @NowUtc;");

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

    private static async Task<MfaChallengeAttemptResult> EvictExhaustedOrExpiredChallengeAsync(
        SqlConnection connection,
        string table,
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var deleteSql = string.Create(
            CultureInfo.InvariantCulture,
            $"DELETE FROM {table} OUTPUT {DeletedColumnsList} WHERE [TokenHash] = @TokenHash;");

        var deletedRow = await connection.QuerySingleOrDefaultAsync<MfaChallengeRow>(
                new CommandDefinition(deleteSql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (deletedRow == null || deletedRow.ExpiresAtUtc <= nowUtc)
        {
            return new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.NotFoundOrExpired, null);
        }

        return new MfaChallengeAttemptResult(MfaChallengeAttemptStatus.Exhausted, deletedRow.ToRecord());
    }

    private static string BuildUpdateOtpStateSql(string table) =>
        $"""
        UPDATE {table}
        SET [SelectedMethod] = @SelectedMethod,
            [OtpCodeHash] = @OtpCodeHash,
            [HashKeyId] = @HashKeyId,
            [ResendCount] = @ResendCount,
            [ResendAvailableAtUtc] = @ResendAvailableAtUtc
        WHERE [TokenHash] = @TokenHash
          AND [ExpiresAtUtc] > @NowUtc
          AND [ResendCount] = @ResendCount - 1
          AND ([ResendAvailableAtUtc] IS NULL OR [ResendAvailableAtUtc] <= @NowUtc);
        """;

    private static string BuildUpsertSql(string table) =>
        $"""
        MERGE {table} WITH (HOLDLOCK) AS target
        USING (SELECT @TokenHash AS [TokenHash]) AS source
        ON target.[TokenHash] = source.[TokenHash]
        WHEN MATCHED THEN
            UPDATE SET
                [ChallengeId] = @ChallengeId,
                [HashKeyId] = @HashKeyId,
                [Purpose] = @Purpose,
                [UserId] = @UserId,
                [SecurityStamp] = @SecurityStamp,
                [AllowedMethods] = @AllowedMethods,
                [SelectedMethod] = @SelectedMethod,
                [OtpCodeHash] = @OtpCodeHash,
                [AttemptCount] = @AttemptCount,
                [MaxAttempts] = @MaxAttempts,
                [ResendCount] = @ResendCount,
                [ResendAvailableAtUtc] = @ResendAvailableAtUtc,
                [CreatedAtUtc] = @CreatedAtUtc,
                [ExpiresAtUtc] = @ExpiresAtUtc
        WHEN NOT MATCHED THEN
            INSERT ({ColumnsList})
            VALUES (@ChallengeId, @TokenHash, @HashKeyId, @Purpose, @UserId, @SecurityStamp, @AllowedMethods, @SelectedMethod, @OtpCodeHash, @AttemptCount, @MaxAttempts, @ResendCount, @ResendAvailableAtUtc, @CreatedAtUtc, @ExpiresAtUtc);
        """;

    private static object ToParameters(MfaChallengeRecord record) =>
        new
        {
            record.ChallengeId,
            record.TokenHash,
            record.HashKeyId,
            Purpose = (byte)record.Purpose,
            record.UserId,
            record.SecurityStamp,
            record.AllowedMethods,
            record.SelectedMethod,
            record.OtpCodeHash,
            record.AttemptCount,
            record.MaxAttempts,
            record.ResendCount,
            record.ResendAvailableAtUtc,
            record.CreatedAtUtc,
            record.ExpiresAtUtc,
        };

    private string GetQualifiedTable()
    {
        var options = _optionsMonitor.CurrentValue;
        return SqlIdentifierValidator.QuoteQualifiedTable(options.SchemaName, options.MfaChallengesTableName);
    }

    internal sealed class MfaChallengeRow
    {
        public Guid ChallengeId { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public string HashKeyId { get; set; } = string.Empty;

        public byte Purpose { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string? SecurityStamp { get; set; }

        public string AllowedMethods { get; set; } = string.Empty;

        public string? SelectedMethod { get; set; }

        public string? OtpCodeHash { get; set; }

        public int AttemptCount { get; set; }

        public int MaxAttempts { get; set; }

        public int ResendCount { get; set; }

        public DateTimeOffset? ResendAvailableAtUtc { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset ExpiresAtUtc { get; set; }

        public MfaChallengeRecord ToRecord() =>
            new(
                ChallengeId,
                TokenHash.TrimEnd(),
                HashKeyId,
                (MfaChallengePurpose)Purpose,
                UserId,
                SecurityStamp,
                AllowedMethods,
                SelectedMethod,
                OtpCodeHash?.TrimEnd(),
                AttemptCount,
                MaxAttempts,
                ResendCount,
                ResendAvailableAtUtc,
                CreatedAtUtc,
                ExpiresAtUtc);
    }
}
