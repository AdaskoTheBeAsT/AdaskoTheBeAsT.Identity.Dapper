using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public interface IJwtClaimCatalogStore
{
    Task<IReadOnlyList<ClaimKindRecord>> GetClaimKindsAsync(
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<ClaimKindRecord> UpsertClaimKindAsync(
        ClaimKindUpsertRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ClaimDefinitionRecord>> GetClaimDefinitionsAsync(
        string? claimKindName,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<ClaimDefinitionRecord> UpsertClaimDefinitionAsync(
        ClaimDefinitionUpsertRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Claim>> FilterAccessTokenClaimsAsync(
        IReadOnlyList<Claim> candidateClaims,
        ISet<string>? allowedKindNames,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ResolvedClaimRecord>> GetUserClaimsForTransformationAsync(
        string userId,
        ISet<string>? allowedKindNames,
        CancellationToken cancellationToken);
}
