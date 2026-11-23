using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Internal;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using Dapper;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Stores;

public sealed class SqlServerTokenRevocationStore : ITokenRevocationStore
{
    private readonly ISqlServerJwtConnectionFactory _connectionFactory;
    private readonly IOptionsMonitor<IdentityJwtSqlServerOptions> _optionsMonitor;
    private readonly TimeProvider _timeProvider;

    public SqlServerTokenRevocationStore(
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

    public async Task RevokeAsync(
        TokenRevocationEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var nowUtc = _timeProvider.GetUtcNow();
        if (entry.ExpiresAtUtc <= nowUtc)
        {
            return;
        }

        var table = GetQualifiedTable();
        var sql = BuildMergeSql(table);

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            await connection.ExecuteAsync(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            entry.Issuer,
                            ScopeType = (byte)entry.SubjectType,
                            entry.SubjectKey,
                            entry.RevokedBeforeUtc,
                            entry.RevokedAtUtc,
                            entry.ExpiresAtUtc,
                            entry.Reason,
                        },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }
    }

    public async Task<bool> IsRevokedAsync(
        TokenRevocationCheckContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var jwtId = string.IsNullOrWhiteSpace(context.JwtId) ? null : context.JwtId;
        var sessionId = string.IsNullOrWhiteSpace(context.SessionId) ? null : context.SessionId;
        var userId = string.IsNullOrWhiteSpace(context.UserId) ? null : context.UserId;

        if (jwtId == null && sessionId == null && userId == null)
        {
            return false;
        }

        var table = GetQualifiedTable();
        var nowUtc = _timeProvider.GetUtcNow();
        var sql = BuildIsRevokedSql(table);

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var matched = await connection.QuerySingleOrDefaultAsync<int?>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            context.Issuer,
                            NowUtc = nowUtc,
                            JwtId = jwtId,
                            SessionId = sessionId,
                            UserId = userId,
                            context.IssuedAtUtc,
                        },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return matched.HasValue;
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

    private static string BuildMergeSql(string table) =>
        $"""
        MERGE {table} WITH (HOLDLOCK) AS target
        USING (SELECT @Issuer AS [Issuer], @ScopeType AS [ScopeType], @SubjectKey AS [SubjectKey]) AS source
        ON target.[Issuer] = source.[Issuer]
           AND target.[ScopeType] = source.[ScopeType]
           AND target.[SubjectKey] = source.[SubjectKey]
        WHEN MATCHED THEN
            UPDATE SET
                [ExpiresAtUtc] = CASE WHEN @ExpiresAtUtc > target.[ExpiresAtUtc] THEN @ExpiresAtUtc ELSE target.[ExpiresAtUtc] END,
                [RevokedBeforeUtc] = CASE
                    WHEN target.[RevokedBeforeUtc] IS NULL THEN @RevokedBeforeUtc
                    WHEN @RevokedBeforeUtc IS NULL THEN target.[RevokedBeforeUtc]
                    WHEN @RevokedBeforeUtc > target.[RevokedBeforeUtc] THEN @RevokedBeforeUtc
                    ELSE target.[RevokedBeforeUtc]
                END,
                [RevokedAtUtc] = @RevokedAtUtc,
                [Reason] = @Reason
        WHEN NOT MATCHED THEN
            INSERT ([Issuer], [ScopeType], [SubjectKey], [RevokedBeforeUtc], [RevokedAtUtc], [ExpiresAtUtc], [Reason])
            VALUES (@Issuer, @ScopeType, @SubjectKey, @RevokedBeforeUtc, @RevokedAtUtc, @ExpiresAtUtc, @Reason);
        """;

    private static string BuildIsRevokedSql(string table) =>
        $"""
        SELECT TOP (1) 1
        FROM {table}
        WHERE [Issuer] = @Issuer
          AND [ExpiresAtUtc] > @NowUtc
          AND (
              (@JwtId IS NOT NULL AND [ScopeType] = 1 AND [SubjectKey] = @JwtId)
              OR (@SessionId IS NOT NULL AND [ScopeType] = 2 AND [SubjectKey] = @SessionId)
              OR (
                  @UserId IS NOT NULL
                  AND [ScopeType] = 3
                  AND [SubjectKey] = @UserId
                  AND (
                      [RevokedBeforeUtc] IS NULL
                      OR @IssuedAtUtc IS NULL
                      OR @IssuedAtUtc <= [RevokedBeforeUtc]
                  )
              )
          );
        """;

    private string GetQualifiedTable()
    {
        var options = _optionsMonitor.CurrentValue;
        return SqlIdentifierValidator.QuoteQualifiedTable(options.SchemaName, options.RevocationsTableName);
    }
}
