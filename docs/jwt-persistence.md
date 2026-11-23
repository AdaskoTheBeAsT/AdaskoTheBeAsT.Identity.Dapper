# JWT persistence and database providers

These packages persist refresh sessions, MFA challenges, OTP replay guards, token revocations, and the claim catalog. They are separate from the Identity source generators and do not replace your `UserManager` store. Install one JWT persistence provider per app. Package names use the `AdaskoTheBeAsT.Identity.Dapper.AspNetCore.<Suffix>` pattern and target .NET 10 and .NET 11. SQL Server uses its own schema and settings; the others share the table layout described below.

| Provider suffix | Issuer registration | Default migration |
| --- | --- | --- |
| `SqlServer` | `AddSqlServerJwtStores` | [`db/SqlServer/IdentityJwt/IdentityJwt.sql`](../db/SqlServer/IdentityJwt/IdentityJwt.sql) |
| `PostgreSql` | `AddPostgreSqlJwtStores` | [`db/PostgreSQL/IdentityJwt.sql`](../db/PostgreSQL/IdentityJwt.sql) |
| `Sqlite` | `AddSqliteJwtStores` | [`db/SQLite/IdentityJwt.sql`](../db/SQLite/IdentityJwt.sql) |
| `Oracle` | `AddOracleJwtStores` | [`db/Oracle/IdentityJwt.sql`](../db/Oracle/IdentityJwt.sql) |
| `MySql` | `AddMySqlJwtStores` | [`db/MySql/IdentityJwt.sql`](../db/MySql/IdentityJwt.sql) |
| `MySqlConnector` | `AddMySqlConnectorJwtStores` | Uses the same MySQL migration |

Apply the migration before starting the app. Alternatively, use the provider's `Schema.<Provider>JwtSchemaHelper` with the same schema and table prefix as registration. For non-SQL Server providers, `BuildSchemaStatements` returns executable statements for a custom deployment; `EnsureSchemaAndTablesAsync` executes them. Both paths seed the default claim kinds without overwriting edits. Run schema setup from a deployment step or one migration process, not concurrently on each replica. The target database or schema must already exist. These migrations do not create ASP.NET Core Identity tables or copy existing JWT state from SQL Server.

```csharp
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.Schema;

// In a migration process:
await PostgreSqlJwtSchemaHelper.EnsureSchemaAndTablesAsync(
    new IdentityJwtPostgreSqlOptions { ConnectionString = connectionString });

// On the issuer, append to AddIdentityJwtApi<ApplicationUser>(...):
issuer.AddPostgreSqlJwtStores(o =>
{
    o.ConnectionString = connectionString;
    o.IdentityUserIdConverter = static id => Guid.Parse(id); // For native UUID columns
});

// On the resource API, append to AddIdentityJwtValidation(...):
validator.AddPostgreSqlTokenRevocationStore(o =>
{
    o.ConnectionString = connectionString;
    o.IdentityUserIdConverter = static id => Guid.Parse(id);
}).AddClaimKindTransformation();
```

For catalog access without database-backed revocation, use `Add<Provider>ClaimCatalogStore` instead. A resource API only needs the existing Identity claim tables if it runs claim transformation. `IdentitySchemaName` and the three Identity claim table options let you point transformation at existing tables; set `IncludeRoleClaimsInTransformation = false` to read only direct user claims. For text identifiers, omit `IdentityUserIdConverter`. For numeric IDs, return the corresponding `int` or `long`. For Oracle `RAW(16)` GUID IDs using the generator's byte layout, use `static id => Guid.Parse(id).ToByteArray()`.

For the non-SQL Server providers, JWT tables use the `identityjwt` prefix by default. All app instances sharing JWT state must use the same schema and prefix. A typed `ConnectionFactory` can replace the connection string; return a new connection on each call, which the store disposes. SQLite requires a native SQLite provider (for example `SQLitePCLRaw.bundle_e_sqlite3` with `SQLitePCL.Batteries.Init()`). Use a file-backed database for durable SQLite storage.

Writes serialize by store through database transactions; SQLite serializes writes at the database level. Cleanup runs every 15 minutes by default, deleting up to 500 expired rows per store per pass. Configure it with `EnableCleanupHostedService`, `CleanupInterval`, and `CleanupBatchSize`; if you disable it on all instances, schedule pruning separately.

## SQL Server

Install `AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer`. Apply the SQL Server migration or use `SqlServerJwtSchemaHelper.EnsureSchemaAndTablesAsync`. `BuildCreateSchemaAndTablesSql` generates the SQL Server migration text. Use the matching `AddSqlServerTokenRevocationStore` or `AddSqlServerClaimCatalogStore` on resource APIs.

## Choosing a store outside the database

- **In memory (default):** Suitable for development, tests, or one process when losing sessions and revocation state on restart is acceptable.
- **Distributed cache:** `AddDistributedCacheStores<TUser, TMfaCache>()` shares revocation and claim caching. Supply an `IAtomicMfaChallengeCache` with shared compare-and-exchange, expiry, and no premature eviction. Plain `IDistributedCache` cannot safely consume a challenge once. Refresh tokens still need a shared `IRefreshTokenStore` for multiple instances.
- **Resource-only validation:** `AddDistributedCacheTokenRevocationStore()` connects a resource API to shared revocation without an MFA cache. An ordinary resource API only validating tokens does not need Identity tables.
