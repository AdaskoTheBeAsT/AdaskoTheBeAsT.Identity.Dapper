using System.Data.Common;
using System.Globalization;
using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal sealed class RelationalClaimCatalog(RelationalDatabase database) : IJwtClaimCatalogStore
{
    private const string Partition = "catalog";
    private readonly string _kinds = database.Table("kinds");
    private readonly string _claims = database.Table("claims");

    public Task<IReadOnlyList<ClaimKindRecord>> GetClaimKindsAsync(bool includeInactive, CancellationToken cancellationToken) =>
        database.ReadAsync<IReadOnlyList<ClaimKindRecord>>(
            async session =>
            {
                var rows = await session.QueryAsync(
                    $"SELECT payload FROM {_kinds} ORDER BY ordinal",
                    ReadKind).ConfigureAwait(false);
                return rows.Where(kind => includeInactive || kind.IsActive).ToArray();
            },
            cancellationToken);

    public Task<ClaimKindRecord> UpsertClaimKindAsync(ClaimKindUpsertRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A claim kind name is required.", nameof(request));
        }

        return database.WriteAsync(Partition, session => SaveKindAsync(session, request), cancellationToken);
    }

    public Task<IReadOnlyList<ClaimDefinitionRecord>> GetClaimDefinitionsAsync(
        string? claimKindName,
        bool includeInactive,
        CancellationToken cancellationToken) =>
        database.ReadAsync<IReadOnlyList<ClaimDefinitionRecord>>(
            async session =>
            {
                var sql = $"SELECT c.payload, k.payload FROM {_claims} c JOIN {_kinds} k ON k.row_key = c.scope_key";
                var rows = string.IsNullOrWhiteSpace(claimKindName)
                    ? await session.QueryAsync(sql + " ORDER BY c.ordinal", ReadDefinition).ConfigureAwait(false)
                    : await session.QueryAsync(sql + " WHERE c.scope_key = @p0 ORDER BY c.ordinal", ReadDefinition, Key(claimKindName)).ConfigureAwait(false);
                return rows.Where(row => includeInactive || (row.Definition.IsActive && row.Kind.IsActive))
                    .Select(row => row.Definition).ToArray();
            },
            cancellationToken);

    public Task<ClaimDefinitionRecord> UpsertClaimDefinitionAsync(ClaimDefinitionUpsertRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ClaimKindName) || string.IsNullOrWhiteSpace(request.ClaimType))
        {
            throw new ArgumentException("A claim kind name and claim type are required.", nameof(request));
        }

        return database.WriteAsync(Partition, session => SaveDefinitionAsync(session, request), cancellationToken);
    }

    public async Task<IReadOnlyList<Claim>> FilterAccessTokenClaimsAsync(
        IReadOnlyList<Claim> candidateClaims,
        ISet<string>? allowedKindNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidateClaims);
        var rows = await database.ReadAsync(
            session => MatchAsync(session, candidateClaims, allowedKindNames, forToken: true),
            cancellationToken).ConfigureAwait(false);
        return rows.Select(row => new Claim(row.ClaimType, row.ClaimValue, row.ValueType)).ToArray();
    }

    public Task<IReadOnlyList<ResolvedClaimRecord>> GetUserClaimsForTransformationAsync(
        string userId,
        ISet<string>? allowedKindNames,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<IReadOnlyList<ResolvedClaimRecord>>(Array.Empty<ResolvedClaimRecord>());
        }

        return database.ReadAsync(
            async session =>
            {
                var userKey = database.Options.IdentityUserIdConverter?.Invoke(userId) ?? userId;
                var source = await session.QueryAsync(BuildIdentityQuery(), static row => new Claim(row.GetString(0), row.GetString(1)), userKey)
                    .ConfigureAwait(false);
                return await MatchAsync(session, source, allowedKindNames, forToken: false).ConfigureAwait(false);
            },
            cancellationToken);
    }

    private static string Key(string value) => RelationalRows.Key(value.ToUpperInvariant());

    private static ClaimKindRecord ReadKind(DbDataReader reader) =>
        RelationalRows.Deserialize<ClaimKindRecord>(reader.GetString(0));

    private static (ClaimDefinitionRecord Definition, ClaimKindRecord Kind) ReadDefinition(DbDataReader reader)
    {
        var stored = RelationalRows.Deserialize<ClaimDefinitionRecord>(reader.GetString(0));
        var kind = RelationalRows.Deserialize<ClaimKindRecord>(reader.GetString(1));
        return (stored with
        {
            ClaimKindName = kind.Name,
            IncludeInAccessToken = kind.IncludeInAccessToken,
            IncludeInTransformation = kind.IncludeInTransformation,
        }, kind);
    }

    private static async Task<int> NextIdAsync(RelationalSession session, string table)
    {
        var result = await session.QueryAsync(
            $"SELECT COALESCE(MAX(ordinal), 0) FROM {table}",
            static row => Convert.ToInt32(row.GetValue(0), CultureInfo.InvariantCulture)).ConfigureAwait(false);
        return checked(result.Single() + 1);
    }

    private async Task<ClaimKindRecord> SaveKindAsync(RelationalSession session, ClaimKindUpsertRequest request)
    {
        var key = Key(request.Name);
        var current = (await RelationalRows.FindAsync<ClaimKindRecord>(session, _kinds, key).ConfigureAwait(false)).SingleOrDefault();
        var id = current?.Id ?? await NextIdAsync(session, _kinds).ConfigureAwait(false);
        var row = new ClaimKindRecord(id, request.Name, request.Description, request.IncludeInAccessToken, request.IncludeInTransformation, request.IsActive);
        if (current == null)
        {
            await RelationalRows.InsertAsync(session, _kinds, key, row, long.MaxValue, ordinal: id).ConfigureAwait(false);
        }
        else
        {
            await RelationalRows.UpdateAsync(session, _kinds, key, row, long.MaxValue).ConfigureAwait(false);
        }

        return row;
    }

    private async Task<ClaimDefinitionRecord> SaveDefinitionAsync(RelationalSession session, ClaimDefinitionUpsertRequest request)
    {
        var kindKey = Key(request.ClaimKindName);
        var kinds = await RelationalRows.FindAsync<ClaimKindRecord>(session, _kinds, kindKey).ConfigureAwait(false);
        var kind = kinds.SingleOrDefault() ?? throw new InvalidOperationException("The requested claim kind does not exist.");
        var key = Key(request.ClaimType);
        var existing = (await RelationalRows.FindAsync<ClaimDefinitionRecord>(session, _claims, key).ConfigureAwait(false)).SingleOrDefault();
        var id = existing?.Id ?? await NextIdAsync(session, _claims).ConfigureAwait(false);
        var type = string.IsNullOrWhiteSpace(request.ValueType) ? ClaimValueTypes.String : request.ValueType;
        var row = new ClaimDefinitionRecord(
            id,
            kind.Id,
            kind.Name,
            request.ClaimType,
            type,
            request.DisplayName,
            request.Description,
            kind.IncludeInAccessToken,
            kind.IncludeInTransformation,
            request.IsActive);
        if (existing == null)
        {
            await RelationalRows.InsertAsync(session, _claims, key, row, long.MaxValue, scope: kindKey, ordinal: id).ConfigureAwait(false);
        }
        else
        {
            await session.ExecuteAsync(
                $"UPDATE {_claims} SET payload = @p1, scope_key = @p2 WHERE row_key = @p0",
                key,
                new JsonPayload(System.Text.Json.JsonSerializer.Serialize(row)),
                kindKey).ConfigureAwait(false);
        }

        return row;
    }

    private async Task<IReadOnlyList<ResolvedClaimRecord>> MatchAsync(
        RelationalSession session,
        IReadOnlyList<Claim> source,
        ISet<string>? allowed,
        bool forToken)
    {
        var definitions = new Dictionary<string, ClaimDefinitionRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in source.Select(claim => Key(claim.Type)).Distinct(StringComparer.Ordinal).Chunk(256))
        {
            var parameters = string.Join(", ", Enumerable.Range(0, chunk.Length).Select(i => "@p" + i.ToString(CultureInfo.InvariantCulture)));
            var rows = await session.QueryAsync(
                $"SELECT c.payload, k.payload FROM {_claims} c JOIN {_kinds} k ON k.row_key = c.scope_key WHERE c.row_key IN ({parameters})",
                ReadDefinition,
                chunk).ConfigureAwait(false);
            foreach (var (definition, kind) in rows)
            {
                if (definition.IsActive && kind.IsActive &&
                    (forToken ? kind.IncludeInAccessToken : kind.IncludeInTransformation) &&
                    (allowed == null || allowed.Count == 0 || allowed.Contains(kind.Name)))
                {
                    definitions.Add(definition.ClaimType, definition);
                }
            }
        }

        var result = new HashSet<ResolvedClaimRecord>();
        foreach (var claim in source)
        {
            if (definitions.TryGetValue(claim.Type, out var definition))
            {
                result.Add(new ResolvedClaimRecord(definition.ClaimType, claim.Value, definition.ValueType, definition.ClaimKindName));
            }
        }

        return result.ToArray();
    }

    private string BuildIdentityQuery()
    {
        var options = database.Options;
        var dialect = database.Dialect;
        var schema = options.IdentitySchemaName ?? options.SchemaName;
        var users = dialect.Table(schema, options.UserClaimsTableName);
        var type = dialect.Column("ClaimType");
        var value = dialect.Column("ClaimValue");
        var user = dialect.Column("UserId");
        var direct = $"SELECT u.{type}, u.{value} FROM {users} u WHERE u.{user} = @p0 AND u.{type} IS NOT NULL AND u.{value} IS NOT NULL";
        if (!options.IncludeRoleClaimsInTransformation)
        {
            return direct;
        }

        var membership = dialect.Table(schema, options.UserRolesTableName);
        var roles = dialect.Table(schema, options.RoleClaimsTableName);
        var role = dialect.Column("RoleId");
        return $"""
            {direct}
            UNION ALL
            SELECT c.{type}, c.{value} FROM {membership} m
            JOIN {roles} c ON c.{role} = m.{role}
            WHERE m.{user} = @p0 AND c.{type} IS NOT NULL AND c.{value} IS NOT NULL
            """;
    }
}
