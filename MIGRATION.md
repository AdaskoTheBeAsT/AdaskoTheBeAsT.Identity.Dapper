# Migrating from 4.x to 5.0.0

Version 5.0.0 changes generated SQL, concurrency behavior, and the example API. Do not mix old generated stores with the new runtime.

## 1. Upgrade and regenerate

- Upgrade `AdaskoTheBeAsT.Identity.Dapper` and your provider package to `5.0.0` together.
- Use C# 11 or later in the consuming project. The runtime still targets `netstandard2.0`.
- Rebuild every project containing generated stores, including libraries deployed with your application. Replace manually retained generated output.
- Install a compatible database driver in the consuming project; provider packages do not supply the driver. Keep provider type-handler initialization from the README.

## 2. Review database mappings

Eligible public read/write custom properties, including inherited properties, now participate in persistence. Add corresponding columns before deploying, or mark properties that must not be persisted with `[NotMapped]`. Review `[Column]` mappings on users, roles, and auxiliary entities.

SQL Server database-generated IDs use the ID column's default. GUID schemas can use `NEWSEQUENTIALID()`. String-key tables require a default or `[InsertOwnId]` with an application-supplied ID. `[InsertOwnId]` is inherited.

Concurrency fixes use existing Identity stamp and token-value columns; they do not require a new version column. Existing null concurrency stamps remain supported.

## 3. Stop assigning replacement concurrency stamps

Pass the stamp loaded from the database into `UpdateAsync` or `DeleteAsync`. Do not generate a replacement stamp before updating. The store rotates the stamp only after a successful update.

A stale or deleted user/role returns an `IdentityResult` containing `ConcurrencyFailure`. Reload the entity and reassess the requested change before retrying; do not blindly retry with stale data.

Token replacement uses an atomic value comparison and can throw `DBConcurrencyException` if another operation changes the token. Repeated writes of the same value are idempotent. Recovery-code redemption atomically consumes codes and prevents concurrent replay.

## 4. Update handwritten SQL and custom stores

Generated consumers receive these contracts automatically. Handwritten SQL implementations must implement:

| Interface | Required SQL behavior |
| --- | --- |
| `IIdentityUserConcurrencySql` | User updates and deletes compare the stored concurrency stamp |
| `IIdentityRoleConcurrencySql` | Role updates and deletes compare the stored concurrency stamp |
| `IIdentityUserTokenConcurrencySql` | `UpdateSql` compares the original token value before replacement |

User/role update SQL receives `OriginalConcurrencyStamp` for the comparison and `ConcurrencyStamp` for the new value. Delete SQL compares the supplied `ConcurrencyStamp`. Token update SQL receives `OriginalValue` for comparison and `Value` for replacement, along with `UserId`, `LoginProvider`, and `Name`.

Use null-safe, exact comparisons for stamps and token values rather than a database's case-insensitive or trailing-space-insensitive text comparison. Preserve database nulls during materialization; do not replace them with Identity constructor defaults.

Review custom protected overrides. Token replacement now uses `TryUpdateTokenImplAsync`, not a remove/add pair. Recovery-code redemption reads and conditionally updates tokens directly, so overriding only `GetTokenAsync` or `ReplaceCodesAsync` no longer customizes that path. Legacy SQL is rejected rather than used for unsafe writes.

Generator extension APIs also changed: `ISourceGeneratorHelper.GenerateCode` accepts the new three-element tuple, some Oracle helper methods require mapping/configuration arguments, and generated SQL no longer calls the old `GenerateUsing` hook. Recompile extensions against the new signatures.

### Paging and claim batching

Generated user and role stores implement `IPagedUserStore<TUser>` and `IPagedRoleStore<TRole>`. Use `GetUsersPageAsync` / `GetRolesPageAsync` for database-side enumeration, with a nonnegative offset, a page size of 1–1000, and cancellation. Ordering uses the mapped ID column; paging is not a snapshot across concurrent changes. Large offsets can still require substantial database work.

The existing `Users` and `Roles` properties still materialize the full table. Applying LINQ `Where`, `Skip`, or `Take` to them does not move those operations into SQL. Migrate large-directory callers to the new methods. If injecting a paging interface directly, register it against the same scoped Identity store instance.

Handwritten SQL must implement `IIdentityUserPagingSql` / `IIdentityRolePagingSql` to support paging; otherwise the new methods throw regeneration guidance. The SQL receives `Offset` and `PageSize`.

Generated claim SQL implements `IIdentityUserClaimBatchSql`. Handwritten claim SQL can opt in with indexed statement templates, create-bind property names, and provider-specific prefix/suffix values, or keep the per-claim fallback. Templates are composite-format strings: `{0}` is the bind-name index, and literal braces must be doubled. Custom stores can override `CreateClaimBatchParameters` for provider-specific typed binding; custom claim factories remain in use.

Batches retain at most 32 claim entities and target at most 900 bind parameters, except when a single entity already exceeds that budget. They do not provide a new transaction spanning every batch. Do not assume failure or cancellation rolls back earlier statements or batches.

Null-aware query plans are cached per entity type, up to 64 distinct schemas. `SqlMapper.SetTypeMap` changes invalidate those plans. Configure maps and handlers at startup; this does not change Dapper's global null-assignment setting or bound its own caches.

## 5. Update example API configuration and clients

- The example retains a publicly known demo signing key and can run locally without a configuration override.
- For a real deployment, override the demo key with a private, cryptographically random `TokenServiceOptions:SigningKey`, at least 32 UTF-8 bytes. The environment-variable name is `TokenServiceOptions__SigningKey`. Never commit a private signing key.
- Changing the signing key invalidates existing access tokens.
- Authenticate user administration requests. User-specific operations require the owner or an `Administrator`; role administration requires an `Administrator`.
- Provision the first administrator through a trusted process. Public registration cannot assign roles.
- JWT `sub` identifies the immutable user ID rather than the username.
- Refresh tokens are single-use and bound to user ID and security stamp. Replace a consumed refresh token with the newly returned token.
- Set `TokenServiceOptions:RefreshTokenCapacity` if the default of 10,000 outstanding tokens is unsuitable. The value must be positive and is captured at service construction. At capacity, issuance evicts the oldest outstanding token, requiring that client to sign in again. Expired/consumed tokens free slots; disposal clears the store.
- Code constructing `TokenService` directly must supply `(TokenServiceOptions, TimeProvider)` instead of an `IMemoryCache`. Register `TimeProvider.System` as a singleton or supply a test clock. The refresh model no longer has `ProtectedTicket`, `RefreshTokenId`, or `IssuedUtc`; no access-token JWT is retained.
- `GET /api/Role` defaults to `offset=0&pageSize=100`, with page sizes limited to 1–1000. Update clients that previously expected every role in one response.
- Invalid password forms return HTTP 400 with `invalid_request`. Unsupported grants, including `client_credentials`, return HTTP 400 with `unsupported_grant_type`.
- The example has no two-factor exchange and does not persist refresh tokens. A multi-instance deployment needs a shared atomic refresh-token store.

## 6. Validate before deployment

Run your application's create/read/update/delete, claim, login, and token tests against its actual database schema. Include stale writes, null stamps, custom fields, and concurrent recovery-code redemption. Generator compilation coverage is not a guarantee for every key/schema/database combination.
