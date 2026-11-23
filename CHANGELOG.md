# Changelog

## 5.0.0 - Unreleased

### Breaking changes

- Upgrade the runtime and provider packages together and rebuild consumers. User, role, and token writes require regenerated concurrency-aware SQL.
- Stores own concurrency-stamp rotation. Stale updates and deletes return `ConcurrencyFailure` rather than overwriting newer data.
- Generated SQL requires C# 11 or later and the generators require Roslyn 4.14 or later. The shared runtime supports `netstandard2.0`, `net10.0`, and `net11.0`.
- The Oracle generator uses `AdaskoTheBeAsT.Dapper.Oracle`. Update imports in custom code that uses its Oracle parameters or type handlers.
- Eligible custom and inherited Identity properties are now persisted. Add their database columns or exclude them with `[NotMapped]`.
- The example API requires authenticated administration. Public registration cannot assign roles; JWT subjects use immutable user IDs and refresh tokens are single-use. Its public demo signing key must be overridden for real deployments.
- The example role-list endpoint returns a page (100 roles by default, at most 1000). Refresh-token retention in the in-memory store is bounded and evicts the oldest outstanding token at capacity. `AdaskoTheBeAsT.Identity.Dapper.WebApi` migrated to built-in `Microsoft.Extensions.DependencyInjection` and `AdaskoTheBeAsT.Identity.Dapper.AspNetCore` (removing external container, mediator, validation, and object-mapping dependencies) while preserving legacy form-encoded `POST /api/token` compatibility alongside `/api/auth/*`.

### Added

- `AdaskoTheBeAsT.Identity.Dapper.MySqlConnector` generates Identity stores for `MySqlConnector.MySqlConnection` and uses the existing MySQL schema scripts. The `MySql.Data` package remains available.
- `AdaskoTheBeAsT.Identity.Dapper.AspNetCore` (`net10.0;net11.0`): reusable ASP.NET Core Identity Minimal API JWT authentication (`AddIdentityJwtApi<TUser>`, `MapIdentityJwtApi<TUser>`), standalone zero-Identity resource-API token validation (`AddIdentityJwtValidation`), `kid`-based asymmetric (`ES256`/`ES384`/`ES512`, `RS256`/`RS384`/`RS512`, `PS256`/`PS384`/`PS512`, `EdDSA`) and symmetric (`HS256`/`HS384`/`HS512`) signing, keyed `HMAC-SHA-256` secret hashing, session-family refresh rotation with replay cascade, shared `jti`/`sid`/`sub` revocation (`FailClosed` / `FailOpen`), and full MFA workflows (2-phase RFC 6238 Authenticator TOTP with Data Protection pending keys and optional QR SVG hook, Email/SMS OTP with challenge-bound single-use verification and resend cooldown, single-use Recovery Codes, out-of-band MFA Setup Links, and `amr` step-up authorization policies).
- `AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer` (`net10.0;net11.0`) and `db/SqlServer/IdentityJwt`: atomic SQL Server Dapper stores (`SqlServerRefreshTokenStore`, `SqlServerMfaChallengeStore`, `SqlServerTokenRevocationStore`), schema helper (`SqlServerJwtSchemaHelper`), and background expired-row cleanup hosted service.
- Added ASP.NET Core JWT persistence packages for PostgreSQL, SQLite, Oracle, and MySQL via separate `MySql.Data` and `MySqlConnector` packages. Each targets `net10.0` and `net11.0` and includes refresh rotation with replay detection, atomic MFA challenge and OTP replay state, token revocation, claim catalog and transformation support, schema setup, and bounded cleanup.
- Authenticator TOTP uses Otp.NET 1.4.1 without changing existing authenticator keys. `.AddMfaQrCodeSvg()` opts into server-rendered SVGs using QRCoder 1.9.0; custom renderers are still supported.
- Multi-targeted ASP.NET Core libraries across `net10.0` and `net11.0` and upgraded SDK/samples/tests to `.NET 11.0.100`.

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
- Distributed MFA challenge operations require a shared `IAtomicMfaChallengeCache`; plain `IDistributedCache` is no longer used for challenges or OTP replay guards. Concurrent resend requests cannot bypass the cooldown or overwrite the winning code.

### Development

- Expanded generator compilation, provider contract, authentication, and package-consumer tests.
- Microsoft.Testing.Platform and provider-specific CI validation.

See [MIGRATION.md](MIGRATION.md) for the 4.x to 5.0.0 upgrade steps.
