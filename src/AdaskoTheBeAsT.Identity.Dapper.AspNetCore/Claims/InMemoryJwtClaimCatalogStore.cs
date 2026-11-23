using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public sealed class InMemoryJwtClaimCatalogStore : IJwtClaimCatalogStore
{
    private readonly Lock _sync = new();
    private readonly Dictionary<int, KindEntry> _kindsById = new();
    private readonly Dictionary<string, int> _kindIdsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DefinitionEntry> _definitionsByType = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Claim>> _userClaims = new(StringComparer.Ordinal);
    private readonly IServiceProvider? _serviceProvider;
    private int _nextKindId;
    private int _nextDefinitionId;

    public InMemoryJwtClaimCatalogStore(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
        SeedDefaultKinds();
    }

    public void SetUserClaims(
        string userId,
        IEnumerable<Claim> claims)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(claims);

        var copy = claims.Select(c => new Claim(c.Type, c.Value, c.ValueType)).ToList();
        lock (_sync)
        {
            _userClaims[userId] = copy;
        }
    }

    public void RemoveUserClaims(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        lock (_sync)
        {
            _userClaims.Remove(userId);
        }
    }

    public Task<IReadOnlyList<ClaimKindRecord>> GetClaimKindsAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var list = _kindsById.Values
                .Where(k => includeInactive || k.IsActive)
                .OrderBy(k => k.Id)
                .Select(ToKindRecord)
                .ToList();
            return Task.FromResult<IReadOnlyList<ClaimKindRecord>>(list);
        }
    }

    public Task<ClaimKindRecord> UpsertClaimKindAsync(
        ClaimKindUpsertRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Claim kind name must be provided.", nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var entry = UpsertKindCore(request);
            return Task.FromResult(ToKindRecord(entry));
        }
    }

    public Task<IReadOnlyList<ClaimDefinitionRecord>> GetClaimDefinitionsAsync(
        string? claimKindName,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var list = _definitionsByType.Values
                .Select(d => (Def: d, Kind: _kindsById[d.ClaimKindId]))
                .Where(x => includeInactive || (x.Def.IsActive && x.Kind.IsActive))
                .Where(x => string.IsNullOrWhiteSpace(claimKindName) ||
                            string.Equals(x.Kind.Name, claimKindName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Def.Id)
                .Select(x => ToDefinitionRecord(x.Def, x.Kind))
                .ToList();
            return Task.FromResult<IReadOnlyList<ClaimDefinitionRecord>>(list);
        }
    }

    public Task<ClaimDefinitionRecord> UpsertClaimDefinitionAsync(
        ClaimDefinitionUpsertRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ClaimKindName) || string.IsNullOrWhiteSpace(request.ClaimType))
        {
            throw new ArgumentException("ClaimKindName and ClaimType must be provided.", nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var valueType = string.IsNullOrWhiteSpace(request.ValueType)
            ? ClaimValueTypes.String
            : request.ValueType;

        lock (_sync)
        {
            if (!_kindIdsByName.TryGetValue(request.ClaimKindName, out var kindId))
            {
                throw new InvalidOperationException($"Claim kind '{request.ClaimKindName}' was not found.");
            }

            var kind = _kindsById[kindId];
            if (!_definitionsByType.TryGetValue(request.ClaimType, out var def))
            {
                def = new DefinitionEntry(++_nextDefinitionId, kindId, request.ClaimType, valueType, request.DisplayName, request.Description, request.IsActive);
                _definitionsByType[request.ClaimType] = def;
            }
            else
            {
                def.ClaimKindId = kindId;
                def.ClaimType = request.ClaimType;
                def.ValueType = valueType;
                def.DisplayName = request.DisplayName;
                def.Description = request.Description;
                def.IsActive = request.IsActive;
            }

            return Task.FromResult(ToDefinitionRecord(def, kind));
        }
    }

    public Task<IReadOnlyList<Claim>> FilterAccessTokenClaimsAsync(
        IReadOnlyList<Claim> candidateClaims,
        ISet<string>? allowedKindNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidateClaims);
        cancellationToken.ThrowIfCancellationRequested();
        if (candidateClaims.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<Claim>>(Array.Empty<Claim>());
        }

        lock (_sync)
        {
            var result = MatchClaimsForAccessToken(candidateClaims, allowedKindNames);
            return Task.FromResult<IReadOnlyList<Claim>>(result);
        }
    }

    public async Task<IReadOnlyList<ResolvedClaimRecord>> GetUserClaimsForTransformationAsync(
        string userId,
        ISet<string>? allowedKindNames,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<ResolvedClaimRecord>();
        }

        var rawClaims = await ResolveRawUserClaimsAsync(userId, cancellationToken).ConfigureAwait(false);
        if (rawClaims.Count == 0)
        {
            return Array.Empty<ResolvedClaimRecord>();
        }

        lock (_sync)
        {
            return MatchClaimsForTransformation(rawClaims, allowedKindNames);
        }
    }

    private static bool IsKindAllowed(
        string kindName,
        ISet<string>? allowedKindNames) =>
        allowedKindNames == null ||
        allowedKindNames.Count == 0 ||
        allowedKindNames.Contains(kindName);

    private static ClaimKindRecord ToKindRecord(KindEntry entry) =>
        new(
            entry.Id,
            entry.Name,
            entry.Description,
            entry.IncludeInAccessToken,
            entry.IncludeInTransformation,
            entry.IsActive);

    private static ClaimDefinitionRecord ToDefinitionRecord(
        DefinitionEntry def,
        KindEntry kind) =>
        new(
            def.Id,
            kind.Id,
            kind.Name,
            def.ClaimType,
            def.ValueType,
            def.DisplayName,
            def.Description,
            kind.IncludeInAccessToken,
            kind.IncludeInTransformation,
            def.IsActive);

    private void SeedDefaultKinds()
    {
        UpsertKindCore(new ClaimKindUpsertRequest(
            IdentityClaimKindNames.FrontendClaim,
            "UI route, guard, and presentation claims included in frontend JWT access tokens.",
            IncludeInAccessToken: true,
            IncludeInTransformation: true,
            IsActive: true));
        UpsertKindCore(new ClaimKindUpsertRequest(
            IdentityClaimKindNames.BackendClaim,
            "Server-side authorization and policy claims enriched during API request transformation.",
            IncludeInAccessToken: false,
            IncludeInTransformation: true,
            IsActive: true));
        UpsertKindCore(new ClaimKindUpsertRequest(
            IdentityClaimKindNames.DataClaim,
            "Row-level, scope, and resource data filter claims enriched during API request transformation.",
            IncludeInAccessToken: false,
            IncludeInTransformation: true,
            IsActive: true));
    }

    private KindEntry UpsertKindCore(ClaimKindUpsertRequest request)
    {
        if (!_kindIdsByName.TryGetValue(request.Name, out var existingId))
        {
            var created = new KindEntry(
                ++_nextKindId,
                request.Name,
                request.Description,
                request.IncludeInAccessToken,
                request.IncludeInTransformation,
                request.IsActive);
            _kindsById[created.Id] = created;
            _kindIdsByName[created.Name] = created.Id;
            return created;
        }

        var entry = _kindsById[existingId];
        _kindIdsByName.Remove(entry.Name);
        entry.Name = request.Name;
        entry.Description = request.Description;
        entry.IncludeInAccessToken = request.IncludeInAccessToken;
        entry.IncludeInTransformation = request.IncludeInTransformation;
        entry.IsActive = request.IsActive;
        _kindIdsByName[entry.Name] = entry.Id;
        return entry;
    }

    private List<Claim> MatchClaimsForAccessToken(
        IReadOnlyList<Claim> candidateClaims,
        ISet<string>? allowedKindNames)
    {
        var seen = new HashSet<(string Type, string Value, string ValueType)>();
        var result = new List<Claim>();

        foreach (var candidate in candidateClaims)
        {
            if (string.IsNullOrWhiteSpace(candidate.Type) ||
                !_definitionsByType.TryGetValue(candidate.Type, out var def) ||
                !def.IsActive)
            {
                continue;
            }

            var kind = _kindsById[def.ClaimKindId];
            if (!kind.IsActive || !kind.IncludeInAccessToken || !IsKindAllowed(kind.Name, allowedKindNames))
            {
                continue;
            }

            var key = (def.ClaimType.ToUpperInvariant(), candidate.Value, def.ValueType);
            if (seen.Add(key))
            {
                result.Add(new Claim(def.ClaimType, candidate.Value, def.ValueType));
            }
        }

        return result;
    }

    private List<ResolvedClaimRecord> MatchClaimsForTransformation(
        IReadOnlyList<Claim> rawClaims,
        ISet<string>? allowedKindNames)
    {
        var seen = new HashSet<(string Type, string Value, string ValueType)>();
        var result = new List<ResolvedClaimRecord>();

        foreach (var candidate in rawClaims)
        {
            if (string.IsNullOrWhiteSpace(candidate.Type) ||
                !_definitionsByType.TryGetValue(candidate.Type, out var def) ||
                !def.IsActive)
            {
                continue;
            }

            var kind = _kindsById[def.ClaimKindId];
            if (!kind.IsActive || !kind.IncludeInTransformation || !IsKindAllowed(kind.Name, allowedKindNames))
            {
                continue;
            }

            var key = (def.ClaimType.ToUpperInvariant(), candidate.Value, def.ValueType);
            if (seen.Add(key))
            {
                result.Add(new ResolvedClaimRecord(def.ClaimType, candidate.Value, def.ValueType, kind.Name));
            }
        }

        return result;
    }

    private async Task<IReadOnlyList<Claim>> ResolveRawUserClaimsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_userClaims.TryGetValue(userId, out var explicitClaims))
            {
                return explicitClaims.ToList();
            }
        }

        if (_serviceProvider?.GetService(typeof(IJwtUserRawClaimsLoader)) is IJwtUserRawClaimsLoader loader)
        {
            return await loader.LoadRawClaimsAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        return Array.Empty<Claim>();
    }

    private sealed class KindEntry(
        int id,
        string name,
        string? description,
        bool includeInAccessToken,
        bool includeInTransformation,
        bool isActive)
    {
        public int Id { get; } = id;

        public string Name { get; set; } = name;

        public string? Description { get; set; } = description;

        public bool IncludeInAccessToken { get; set; } = includeInAccessToken;

        public bool IncludeInTransformation { get; set; } = includeInTransformation;

        public bool IsActive { get; set; } = isActive;
    }

    private sealed class DefinitionEntry(
        int id,
        int claimKindId,
        string claimType,
        string valueType,
        string? displayName,
        string? description,
        bool isActive)
    {
        public int Id { get; } = id;

        public int ClaimKindId { get; set; } = claimKindId;

        public string ClaimType { get; set; } = claimType;

        public string ValueType { get; set; } = valueType;

        public string? DisplayName { get; set; } = displayName;

        public string? Description { get; set; } = description;

        public bool IsActive { get; set; } = isActive;
    }
}
