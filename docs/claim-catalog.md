# Claim catalogs and request enrichment

Use claim kinds to decide which Identity claims travel in an access token and which are read by an API. A **definition** classifies a claim type; it does not grant that claim. Continue assigning values through `UserManager.AddClaimAsync`, role claims, or your Identity administration workflow.

| Seeded kind | In access token | In transformation | Example |
| --- | --- | --- | --- |
| `FrontendClaim` | Yes | Yes | `ui:reports:view`, a navigation hint |
| `BackendClaim` | No | Yes | `api:reports:read`, checked by an API policy |
| `DataClaim` | No | Yes | `data:branch_id`, used in query filters |

## Configure and populate

1. Apply the [provider migration](jwt-persistence.md). It seeds the three kinds; it does not create claim definitions or assign user/role claims.
2. On an issuer, chain `.AddClaimKindSupport()` onto `AddIdentityJwtApi<TUser>()` to enable token filtering and request transformation. If you only need one of them, set the other flag to `false` in the configuration callback. Configure `AccessTokenClaimKinds` and `TransformationClaimKinds` to restrict each path to specific kinds. `IncludeRolesInAccessToken` controls separate role emission.
3. In a resource-only API, chain `.AddClaimKindTransformation()` onto `AddIdentityJwtValidation()` and register the database catalog (`.AddPostgreSqlClaimCatalogStore(...)`, for example). No `UserManager` is required, but the database store must be pointed at the correct existing Identity claim and membership tables.

In a migration or authorized administration service:

```csharp
using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

await catalog.UpsertClaimDefinitionAsync(
    new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.FrontendClaim, "ui:reports:view"),
    cancellationToken);
await catalog.UpsertClaimDefinitionAsync(
    new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.BackendClaim, "api:reports:read"),
    cancellationToken);
await catalog.UpsertClaimDefinitionAsync(
    new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.DataClaim, "data:branch_id", ClaimValueTypes.Integer32),
    cancellationToken);

// A definition does not add any of these values to a user.
await userManager.AddClaimAsync(user, new Claim("api:reports:read", "true"));
await userManager.AddClaimAsync(user, new Claim("data:branch_id", "42"));
```

In the resource API:

```csharp
app.MapGet("/api/report-scope", (ClaimsPrincipal user) =>
{
    var branchIds = user.GetInt32ClaimValues("data:branch_id");
    return branchIds.Count == 0 ? Results.Forbid() : Results.Ok(branchIds);
}).RequireAuthorization(policy => policy.RequireClaim("api:reports:read", "true"));
```

Apply these scopes to your actual database query; a claim definition does not enforce a row filter. Treat an empty set as no access, not unrestricted access.

## Cache changes and troubleshoot

- Cache duration defaults to two minutes. After a direct claim or membership change, call `IJwtClaimTransformationCache.InvalidateUserAsync(userId)` after the write commits. Role-claim or catalog edits can affect several users; invalidate affected users or call `InvalidateAllAsync()`. In a multi-node setup use shared invalidation or account for the TTL on other nodes.
- Invalidation affects enriched claims, not claims already signed into existing tokens. Newly issued tokens reflect current catalog settings. Revoke affected sessions or tokens if a stale token must stop working immediately.
- Missing claim? Check the active kind and definition, matching claim name, assigned values, transformation kind filter, Identity schema/table names, and the configured ID conversion. See the [provider guide](jwt-persistence.md).
