# Changelog

## 5.0.0 - Unreleased

### Breaking changes

- Upgrade the runtime and provider packages together and rebuild consumers. User, role, and token writes require regenerated concurrency-aware SQL.
- Stores own concurrency-stamp rotation. Stale updates and deletes return `ConcurrencyFailure` rather than overwriting newer data.
- Generated SQL requires C# 11 or later. The runtime remains `netstandard2.0`.
- Eligible custom and inherited Identity properties are now persisted. Add their database columns or exclude them with `[NotMapped]`.
- The example API requires authenticated administration. Public registration cannot assign roles; JWT subjects use immutable user IDs and refresh tokens are single-use. Its public demo signing key must be overridden for real deployments.
- The example role-list endpoint returns a page (100 roles by default, at most 1000). Refresh-token retention is bounded and can evict the oldest outstanding token. Direct `TokenService` construction now requires a `TimeProvider`; unused refresh-token model fields were removed.

### Performance

- Added asynchronous database-side user and role paging with bounded page sizes and mapped ID ordering. Existing `Users`/`Roles` LINQ collections remain eager for compatibility.
- Bounded the example's isolated refresh-token store to a configurable capacity, defaulting to 10,000, with atomic consumption, expiry cleanup, disposal, and no retained access-token JWTs.
- Cached up to 64 null-aware materializer plans per entity type, with compiled null setters and invalidation when the Dapper type map changes.
- Batched user claim additions and removals across all five providers, preserving typed custom properties and a legacy per-claim fallback.

### Fixed

- Recovery-code replay, concurrent redemption, and empty-code acceptance.
- Database-null stamp materialization, roleless-user enumeration, custom mappings, inherited properties, and semantic key-type resolution.
- Runtime package dependencies and clean-package consumer loading.
- MySQL repeated token writes with `UseAffectedRows=true`, including null and empty values.
- Oracle named binding, claim-replacement parameters, scoped-login lookup, and escaped custom-property identifiers.
- Invalid password-token requests returning HTTP 500 instead of HTTP 400.
- Generated raw SQL literals now preserve all C# line breaks, including U+0085, U+2028, and U+2029.
- Example API startup rejects the public demo signing key outside `Development`.

### Development

- Expanded generator compilation, provider contract, authentication, and package-consumer tests.
- Microsoft.Testing.Platform and provider-specific CI validation.

See [MIGRATION.md](MIGRATION.md) for the 4.x to 5.0.0 upgrade steps.
