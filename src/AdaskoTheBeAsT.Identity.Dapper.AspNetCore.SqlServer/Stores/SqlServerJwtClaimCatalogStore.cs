using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Internal;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using Dapper;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Stores;

public sealed class SqlServerJwtClaimCatalogStore : IJwtClaimCatalogStore
{
    private readonly ISqlServerJwtConnectionFactory _connectionFactory;
    private readonly IOptionsMonitor<IdentityJwtSqlServerOptions> _optionsMonitor;

    public SqlServerJwtClaimCatalogStore(
        ISqlServerJwtConnectionFactory connectionFactory,
        IOptionsMonitor<IdentityJwtSqlServerOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        _connectionFactory = connectionFactory;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<IReadOnlyList<ClaimKindRecord>> GetClaimKindsAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var claimKindsTable = GetClaimKindsTable();
        var sql = string.Create(
            CultureInfo.InvariantCulture,
            $"SELECT [Id], [Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive] FROM {claimKindsTable} WHERE (@IncludeInactive = 1 OR [IsActive] = 1) ORDER BY [Id] ASC;");

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var rows = await connection.QueryAsync<ClaimKindRecord>(
                    new CommandDefinition(
                        sql,
                        new { IncludeInactive = includeInactive ? 1 : 0 },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            return rows.AsList();
        }
    }

    public async Task<ClaimKindRecord> UpsertClaimKindAsync(
        ClaimKindUpsertRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Claim kind name must be provided.", nameof(request));
        }

        var claimKindsTable = GetClaimKindsTable();
        var sql = BuildUpsertClaimKindSql(claimKindsTable);

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            return await connection.QuerySingleAsync<ClaimKindRecord>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            request.Name,
                            request.Description,
                            request.IncludeInAccessToken,
                            request.IncludeInTransformation,
                            request.IsActive,
                        },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<ClaimDefinitionRecord>> GetClaimDefinitionsAsync(
        string? claimKindName,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var claimKindsTable = GetClaimKindsTable();
        var claimsTable = GetClaimsTable();
        var sql = BuildGetDefinitionsSql(claimsTable, claimKindsTable);
        var normalizedKind = string.IsNullOrWhiteSpace(claimKindName) ? null : claimKindName;

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var rows = await connection.QueryAsync<ClaimDefinitionRecord>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            IncludeInactive = includeInactive ? 1 : 0,
                            ClaimKindName = normalizedKind,
                        },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            return rows.AsList();
        }
    }

    public async Task<ClaimDefinitionRecord> UpsertClaimDefinitionAsync(
        ClaimDefinitionUpsertRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ClaimKindName) || string.IsNullOrWhiteSpace(request.ClaimType))
        {
            throw new ArgumentException("ClaimKindName and ClaimType must be provided.", nameof(request));
        }

        var valueType = string.IsNullOrWhiteSpace(request.ValueType)
            ? ClaimValueTypes.String
            : request.ValueType;

        var claimKindsTable = GetClaimKindsTable();
        var claimsTable = GetClaimsTable();
        var sql = BuildUpsertDefinitionSql(claimsTable, claimKindsTable);

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var record = await connection.QuerySingleOrDefaultAsync<ClaimDefinitionRecord>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            request.ClaimKindName,
                            request.ClaimType,
                            ValueType = valueType,
                            request.DisplayName,
                            request.Description,
                            request.IsActive,
                        },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return record ?? throw new InvalidOperationException(
                $"Claim kind '{request.ClaimKindName}' was not found.");
        }
    }

    public async Task<IReadOnlyList<Claim>> FilterAccessTokenClaimsAsync(
        IReadOnlyList<Claim> candidateClaims,
        ISet<string>? allowedKindNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidateClaims);
        if (candidateClaims.Count == 0)
        {
            return Array.Empty<Claim>();
        }

        var distinctTypes = ExtractDistinctClaimTypes(candidateClaims);
        if (distinctTypes.Count == 0)
        {
            return Array.Empty<Claim>();
        }

        var matchedByType = await QueryAccessTokenDefinitionsAsync(distinctTypes, allowedKindNames, cancellationToken)
            .ConfigureAwait(false);
        if (matchedByType.Count == 0)
        {
            return Array.Empty<Claim>();
        }

        return BuildFilteredAccessTokenClaims(candidateClaims, matchedByType);
    }

    public async Task<IReadOnlyList<ResolvedClaimRecord>> GetUserClaimsForTransformationAsync(
        string userId,
        ISet<string>? allowedKindNames,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<ResolvedClaimRecord>();
        }

        var options = _optionsMonitor.CurrentValue;
        var filterByKindNames = allowedKindNames != null && allowedKindNames.Count > 0;
        var sql = BuildTransformationQuerySql(options, filterByKindNames);
        var kindList = filterByKindNames ? allowedKindNames!.ToArray() : Array.Empty<string>();

        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var rows = await connection.QueryAsync<ResolvedClaimRecord>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            UserId = new DbString
                            {
                                Value = userId,
                                IsAnsi = false,
                                Length = 450,
                            },
                            AllowedKindNames = kindList,
                        },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return DeduplicateResolvedClaims(rows);
        }
    }

    private static List<string> ExtractDistinctClaimTypes(IReadOnlyList<Claim> candidateClaims) =>
        candidateClaims
            .Select(claim => claim.Type)
            .Where(type => !string.IsNullOrWhiteSpace(type) && type.Length <= 256)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<Claim> BuildFilteredAccessTokenClaims(
        IReadOnlyList<Claim> candidateClaims,
        Dictionary<string, AccessTokenDefRow> matchedByType)
    {
        var seen = new HashSet<(string Type, string Value, string ValueType)>();
        var filtered = new List<Claim>();

        foreach (var candidate in candidateClaims)
        {
            if (string.IsNullOrWhiteSpace(candidate.Type) ||
                !matchedByType.TryGetValue(candidate.Type, out var matched))
            {
                continue;
            }

            var valueType = string.IsNullOrWhiteSpace(matched.ValueType)
                ? ClaimValueTypes.String
                : matched.ValueType;
            var key = (matched.ClaimType.ToUpperInvariant(), candidate.Value, valueType);
            if (seen.Add(key))
            {
                filtered.Add(new Claim(matched.ClaimType, candidate.Value, valueType));
            }
        }

        return filtered;
    }

    private static List<ResolvedClaimRecord> DeduplicateResolvedClaims(IEnumerable<ResolvedClaimRecord> rows)
    {
        var seen = new HashSet<(string Type, string Value, string ValueType)>();
        var result = new List<ResolvedClaimRecord>();

        foreach (var row in rows)
        {
            var valueType = string.IsNullOrWhiteSpace(row.ValueType)
                ? ClaimValueTypes.String
                : row.ValueType;
            var key = (row.ClaimType.ToUpperInvariant(), row.ClaimValue, valueType);
            if (seen.Add(key))
            {
                result.Add(row with { ValueType = valueType });
            }
        }

        return result;
    }

    private static string BuildUpsertClaimKindSql(string claimKindsTable) =>
        $"""
        MERGE {claimKindsTable} WITH (HOLDLOCK) AS target
        USING (SELECT @Name AS [Name]) AS source
        ON target.[Name] = source.[Name]
        WHEN MATCHED THEN
            UPDATE SET
                [Description] = @Description,
                [IncludeInAccessToken] = @IncludeInAccessToken,
                [IncludeInTransformation] = @IncludeInTransformation,
                [IsActive] = @IsActive
        WHEN NOT MATCHED THEN
            INSERT ([Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive])
            VALUES (@Name, @Description, @IncludeInAccessToken, @IncludeInTransformation, @IsActive);

        SELECT TOP (1)
            [Id],
            [Name],
            [Description],
            [IncludeInAccessToken],
            [IncludeInTransformation],
            [IsActive]
        FROM {claimKindsTable}
        WHERE [Name] = @Name;
        """;

    private static string BuildGetDefinitionsSql(
        string claimsTable,
        string claimKindsTable) =>
        $"""
        SELECT
            c.[Id],
            c.[ClaimKindId],
            ck.[Name] AS [ClaimKindName],
            c.[ClaimType],
            c.[ValueType],
            c.[DisplayName],
            c.[Description],
            ck.[IncludeInAccessToken],
            ck.[IncludeInTransformation],
            c.[IsActive]
        FROM {claimsTable} AS c
        INNER JOIN {claimKindsTable} AS ck ON ck.[Id] = c.[ClaimKindId]
        WHERE (@IncludeInactive = 1 OR (c.[IsActive] = 1 AND ck.[IsActive] = 1))
          AND (@ClaimKindName IS NULL OR ck.[Name] = @ClaimKindName)
        ORDER BY c.[Id] ASC;
        """;

    private static string BuildUpsertDefinitionSql(
        string claimsTable,
        string claimKindsTable) =>
        $"""
        DECLARE @KindId INT = (
            SELECT TOP (1) [Id]
            FROM {claimKindsTable}
            WHERE [Name] = @ClaimKindName
        );

        IF @KindId IS NOT NULL
        BEGIN
            MERGE {claimsTable} WITH (HOLDLOCK) AS target
            USING (SELECT @ClaimType AS [ClaimType]) AS source
            ON target.[ClaimType] = source.[ClaimType]
            WHEN MATCHED THEN
                UPDATE SET
                    [ClaimKindId] = @KindId,
                    [ValueType] = @ValueType,
                    [DisplayName] = @DisplayName,
                    [Description] = @Description,
                    [IsActive] = @IsActive
            WHEN NOT MATCHED THEN
                INSERT ([ClaimKindId], [ClaimType], [ValueType], [DisplayName], [Description], [IsActive])
                VALUES (@KindId, @ClaimType, @ValueType, @DisplayName, @Description, @IsActive);

            SELECT TOP (1)
                c.[Id],
                c.[ClaimKindId],
                ck.[Name] AS [ClaimKindName],
                c.[ClaimType],
                c.[ValueType],
                c.[DisplayName],
                c.[Description],
                ck.[IncludeInAccessToken],
                ck.[IncludeInTransformation],
                c.[IsActive]
            FROM {claimsTable} AS c
            INNER JOIN {claimKindsTable} AS ck ON ck.[Id] = c.[ClaimKindId]
            WHERE c.[ClaimType] = @ClaimType;
        END;
        """;

    private static string BuildAccessTokenDefinitionsSql(
        string claimsTable,
        string claimKindsTable,
        bool filterByKindNames)
    {
        var kindPredicate = filterByKindNames ? "AND ck.[Name] IN @AllowedKindNames" : string.Empty;
        return $"""
            SELECT
                c.[ClaimType],
                c.[ValueType]
            FROM {claimsTable} AS c
            INNER JOIN {claimKindsTable} AS ck ON ck.[Id] = c.[ClaimKindId]
            WHERE c.[IsActive] = 1
              AND ck.[IsActive] = 1
              AND ck.[IncludeInAccessToken] = 1
              AND c.[ClaimType] IN @ClaimTypes
              {kindPredicate};
            """;
    }

    private static string BuildTransformationQuerySql(
        IdentityJwtSqlServerOptions options,
        bool filterByKindNames)
    {
        var claimKindsTable = SqlIdentifierValidator.QuoteQualifiedTable(options.SchemaName, options.ClaimKindsTableName);
        var claimsTable = SqlIdentifierValidator.QuoteQualifiedTable(options.SchemaName, options.ClaimsTableName);
        var identitySchema = string.IsNullOrWhiteSpace(options.IdentitySchemaName)
            ? options.SchemaName
            : options.IdentitySchemaName;
        var userClaimsTable = SqlIdentifierValidator.QuoteQualifiedTable(identitySchema, options.UserClaimsTableName);
        var kindPredicate = filterByKindNames ? "AND ck.[Name] IN @AllowedKindNames" : string.Empty;

        var sourceSql = options.IncludeRoleClaimsInTransformation
            ? BuildUserAndRoleSourceSql(
                userClaimsTable,
                SqlIdentifierValidator.QuoteQualifiedTable(identitySchema, options.UserRolesTableName),
                SqlIdentifierValidator.QuoteQualifiedTable(identitySchema, options.RoleClaimsTableName))
            : BuildUserOnlySourceSql(userClaimsTable);

        return $"""
            SELECT
                c.[ClaimType] AS [ClaimType],
                src.[ClaimValue] AS [ClaimValue],
                c.[ValueType] AS [ValueType],
                ck.[Name] AS [ClaimKindName]
            FROM (
                {sourceSql}
            ) AS src
            INNER JOIN {claimsTable} AS c
                ON c.[ClaimType] = src.[ClaimType]
               AND c.[IsActive] = 1
            INNER JOIN {claimKindsTable} AS ck
                ON ck.[Id] = c.[ClaimKindId]
               AND ck.[IsActive] = 1
               AND ck.[IncludeInTransformation] = 1
               {kindPredicate}
            ORDER BY c.[Id] ASC;
            """;
    }

    private static string BuildUserOnlySourceSql(string userClaimsTable) =>
        $"""
        SELECT uc.[ClaimType], uc.[ClaimValue]
        FROM {userClaimsTable} AS uc
        WHERE uc.[UserId] = @UserId
          AND uc.[ClaimType] IS NOT NULL
          AND uc.[ClaimValue] IS NOT NULL
        """;

    private static string BuildUserAndRoleSourceSql(
        string userClaimsTable,
        string userRolesTable,
        string roleClaimsTable) =>
        $"""
        SELECT uc.[ClaimType], uc.[ClaimValue]
        FROM {userClaimsTable} AS uc
        WHERE uc.[UserId] = @UserId
          AND uc.[ClaimType] IS NOT NULL
          AND uc.[ClaimValue] IS NOT NULL
        UNION ALL
        SELECT rc.[ClaimType], rc.[ClaimValue]
        FROM {userRolesTable} AS ur
        INNER JOIN {roleClaimsTable} AS rc ON rc.[RoleId] = ur.[RoleId]
        WHERE ur.[UserId] = @UserId
          AND rc.[ClaimType] IS NOT NULL
          AND rc.[ClaimValue] IS NOT NULL
        """;

    private async Task<Dictionary<string, AccessTokenDefRow>> QueryAccessTokenDefinitionsAsync(
        List<string> distinctTypes,
        ISet<string>? allowedKindNames,
        CancellationToken cancellationToken)
    {
        var claimKindsTable = GetClaimKindsTable();
        var claimsTable = GetClaimsTable();
        var filterByKindNames = allowedKindNames != null && allowedKindNames.Count > 0;
        var sql = BuildAccessTokenDefinitionsSql(claimsTable, claimKindsTable, filterByKindNames);

        var kindList = filterByKindNames ? allowedKindNames!.ToArray() : Array.Empty<string>();
        var connection = _connectionFactory.CreateConnection();
        await using (connection.ConfigureAwait(false))
        {
            await SqlServerJwtConnectionFactory.EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            var rows = await connection.QueryAsync<AccessTokenDefRow>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            ClaimTypes = distinctTypes,
                            AllowedKindNames = kindList,
                        },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var dict = new Dictionary<string, AccessTokenDefRow>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                dict[row.ClaimType] = row;
            }

            return dict;
        }
    }

    private string GetClaimKindsTable()
    {
        var options = _optionsMonitor.CurrentValue;
        return SqlIdentifierValidator.QuoteQualifiedTable(options.SchemaName, options.ClaimKindsTableName);
    }

    private string GetClaimsTable()
    {
        var options = _optionsMonitor.CurrentValue;
        return SqlIdentifierValidator.QuoteQualifiedTable(options.SchemaName, options.ClaimsTableName);
    }

    private sealed record AccessTokenDefRow(
        string ClaimType,
        string ValueType);
}
