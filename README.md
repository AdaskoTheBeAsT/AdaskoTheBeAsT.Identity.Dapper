# 🚀 AdaskoTheBeAsT.Identity.Dapper

[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/AdaskoTheBeAsT.Identity.Dapper.svg)](https://www.nuget.org/packages/AdaskoTheBeAsT.Identity.Dapper/)
[![SDK](https://img.shields.io/badge/SDK-.NET%2010.0.401-512BD4.svg)](https://dotnet.microsoft.com/)
[![Target](https://img.shields.io/badge/target-netstandard2.0-7f52ff.svg)](https://dotnet.microsoft.com/)

> Compile-time ASP.NET Core Identity stores for Dapper.
>
> If you like ASP.NET Core Identity but do not want EF Core in the store layer, this repository gives you source-generated stores and SQL for SQL Server, PostgreSQL, MySQL, Oracle, and SQLite.

## ✨ Why developers like it

- ⚡ Fast runtime path powered by Dapper
- 🧠 Source-generated stores and provider-specific SQL instead of hand-written plumbing
- 🧩 Works with `string`, `int`, `long`, and `Guid` keys
- 🏗️ Supports custom Identity properties and custom column names
- 🗄️ Covers SQL Server, PostgreSQL, MySQL, Oracle, and SQLite
- 🎛️ Lets you skip normalized columns when you do not need them
- 🧪 Backed by provider-specific unit and integration tests

## 🗄️ Supported providers

| Provider | NuGet package | Default schema | Extra startup step |
| --- | --- | --- | --- |
| SQL Server | `AdaskoTheBeAsT.Identity.Dapper.SqlServer` | `dbo` | none |
| PostgreSQL | `AdaskoTheBeAsT.Identity.Dapper.PostgreSql` | `public` | `PostgreSqlDapperConfig.ConfigureTypeHandlers();` |
| MySQL | `AdaskoTheBeAsT.Identity.Dapper.MySql` | n/a | `MySqlDapperConfig.ConfigureTypeHandlers();` |
| Oracle | `AdaskoTheBeAsT.Identity.Dapper.Oracle` | empty by default | `OracleDapperConfig.ConfigureTypeHandlers();` |
| SQLite | `AdaskoTheBeAsT.Identity.Dapper.Sqlite` | n/a | `SQLitePCL.Batteries.Init();` and `SqliteDapperConfig.ConfigureTypeHandlers();` |

The shared runtime package is `AdaskoTheBeAsT.Identity.Dapper`; most applications install a provider package and let that pull in the core runtime.

Consuming projects need C# 11 or later because generated SQL properties use raw string literals. The runtime library still targets `netstandard2.0`.

## ⚡ Quick start

### 1. Install the provider package and its database driver

The provider package pulls in the shared runtime, Dapper, and Identity stores.
The database driver must be installed in the consuming project. These are the
driver versions used by the tests; choose compatible versions for your app.
The Oracle provider also brings in `Dapper.Oracle`.

```bash
# SQL Server
dotnet add package AdaskoTheBeAsT.Identity.Dapper.SqlServer
dotnet add package Microsoft.Data.SqlClient --version 7.1.1

# PostgreSQL
dotnet add package AdaskoTheBeAsT.Identity.Dapper.PostgreSql
dotnet add package Npgsql --version 10.0.3

# MySQL
dotnet add package AdaskoTheBeAsT.Identity.Dapper.MySql
dotnet add package MySql.Data --version 26.7.0

# Oracle
dotnet add package AdaskoTheBeAsT.Identity.Dapper.Oracle
dotnet add package Oracle.ManagedDataAccess.Core --version 23.26.301

# SQLite
dotnet add package AdaskoTheBeAsT.Identity.Dapper.Sqlite
dotnet add package Microsoft.Data.Sqlite.Core --version 10.0.12
dotnet add package SQLitePCLRaw.bundle_e_sqlite3 --version 3.0.5
```

### 2. Define your Identity types

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using AdaskoTheBeAsT.Identity.Dapper.Attributes;
using Microsoft.AspNetCore.Identity;

namespace MyApp.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
}

public sealed class ApplicationRoleClaim : IdentityRoleClaim<Guid>
{
}

[InsertOwnId]
public sealed class ApplicationUser : IdentityUser<Guid>
{
    [Column("IsActive")]
    public bool IsActive { get; set; }
}

public sealed class ApplicationUserClaim : IdentityUserClaim<Guid>
{
}

public sealed class ApplicationUserLogin : IdentityUserLogin<Guid>
{
}

public sealed class ApplicationUserRole : IdentityUserRole<Guid>
{
}

public sealed class ApplicationUserToken : IdentityUserToken<Guid>
{
}
```

`[InsertOwnId]` is optional and useful when you want to keep external identity IDs unchanged.
For MySQL-generated string or GUID IDs without `[InsertOwnId]`, enable `AllowUserVariables=true` in the connection string.

### 3. Add optional MSBuild settings

Provider packages already ship sensible defaults. Add overrides only when you need them:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  <AdaskoTheBeAsTIdentityDapper_SkipNormalized>false</AdaskoTheBeAsTIdentityDapper_SkipNormalized>
  <AdaskoTheBeAsTIdentityDapper_DbSchema>dbo</AdaskoTheBeAsTIdentityDapper_DbSchema>
</PropertyGroup>
```

### 4. Register the connection provider and Identity stores

The example below uses SQL Server, but the pattern is the same for other providers.

```csharp
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

builder.Services.AddSingleton<IIdentityDbConnectionProvider<SqlConnection>, IdentityDbConnectionProvider>();

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddUserStore<ApplicationUserStore>()
    .AddRoleStore<ApplicationRoleStore>()
    .AddDefaultTokenProviders();

public sealed class IdentityDbConnectionProvider : IIdentityDbConnectionProvider<SqlConnection>
{
    private readonly IConfiguration _configuration;

    public IdentityDbConnectionProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SqlConnection Provide() => new(_configuration.GetConnectionString("DefaultConnection")!);
}
```

`ApplicationUserStore`, `ApplicationUserOnlyStore`, and `ApplicationRoleStore` are generated during build.

### 5. Build once to generate the stores

```bash
dotnet build
```

If `EmitCompilerGeneratedFiles` is enabled, generated files are written to the folder configured by `CompilerGeneratedFilesOutputPath`.

### 6. Create the database schema

This library generates store code and SQL access logic, but it does not create your database schema for you.

Use the scripts in `db/`:

- `db/SqlServer/` contains SSDT-style `.sqlproj` schema projects
- `db/PostgreSQL/`, `db/MySql/`, `db/Oracle/`, and `db/SQLite/` contain SQL scripts
- scripts are available for `string`, `int`, `bigint`, and `Guid` keys
- each provider includes normalized and `WithoutNormalized...` variants

## ⚙️ Configuration reference

| Property | Default | Applies to | What it changes |
| --- | --- | --- | --- |
| `EmitCompilerGeneratedFiles` | `false` | all providers | writes generated code to disk |
| `CompilerGeneratedFilesOutputPath` | `Generated` | all providers | changes the generated output folder |
| `AdaskoTheBeAsTIdentityDapper_SkipNormalized` | `false` | all providers | skips normalized role and user columns |
| `AdaskoTheBeAsTIdentityDapper_DbSchema` | `dbo` / `public` / empty | SQL Server, PostgreSQL, Oracle | changes the schema prefix used by generated SQL |
| `AdaskoTheBeAsTIdentityDapper_StoreBooleanAs` | `char` | Oracle only | stores booleans as `char`, `number`, or `string` |

### Provider notes

- PostgreSQL: call `PostgreSqlDapperConfig.ConfigureTypeHandlers();`
- MySQL: call `MySqlDapperConfig.ConfigureTypeHandlers();`
- Oracle: call `OracleDapperConfig.ConfigureTypeHandlers();`
- SQLite: call `SQLitePCL.Batteries.Init();` and `SqliteDapperConfig.ConfigureTypeHandlers();`

## Bounded reads and claim writes

Use `IPagedUserStore<TUser>.GetUsersPageAsync` and `IPagedRoleStore<TRole>.GetRolesPageAsync` for large directories. Generated stores implement both capabilities on their respective store types:

```csharp
var store = (IPagedUserStore<ApplicationUser>)serviceProvider
    .GetRequiredService<IUserStore<ApplicationUser>>();
var users = await store.GetUsersPageAsync(
    offset: 0, pageSize: 100, cancellationToken: cancellationToken);
```

Paging runs in the database, orders by the mapped ID column, accepts nonnegative offsets, and limits page sizes to 1–1000. Register the paging interfaces separately if you want to inject them directly. Large offsets can still be expensive, and concurrent changes can move rows between pages.

Identity's existing `Users` and `Roles` properties remain eager, in-memory `IQueryable` collections for compatibility. Calling `Where`, `Skip`, or `Take` on them does **not** limit the database query. Switch enumeration code to the explicit paging APIs to bound memory.

User claim additions and removals use batches of at most 32 claims, reduced as needed toward a 900-bind-parameter budget. The batches preserve custom claim factories and typed null parameters. Handwritten SQL without `IIdentityUserClaimBatchSql` keeps the per-claim path. Batching does not introduce an all-chunks transaction: earlier work may remain if a later statement fails or the operation is cancelled.

Null-aware materializers cache up to 64 schema plans per entity type and compile null setters once per plan. Changing a Dapper type map invalidates these plans. This bounds this library's plan cache, not Dapper's separate caches.

## 📚 Examples and useful docs

- [`samples/Sample.SqlServer2`](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/tree/main/samples/Sample.SqlServer2) — SQL Server sample that consumes the NuGet packages
- [`samples/Sample.SqlServer`](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/tree/main/samples/Sample.SqlServer) — SQL Server sample that references local projects
- [`src/AdaskoTheBeAsT.Identity.Dapper.WebApi`](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/tree/main/src/AdaskoTheBeAsT.Identity.Dapper.WebApi) — runnable Web API example
- [`samples/OracleConsoleApp/PrepareOracleDb.txt`](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/blob/main/samples/OracleConsoleApp/PrepareOracleDb.txt) — Oracle prep notes
- [`db/`](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/tree/main/db) — provider schema scripts and SQL Server schema projects

## Upgrading generated stores to 5.0.0

Version 5.0.0 is a breaking release. See the [migration guide](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/blob/main/MIGRATION.md) and [changelog](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/blob/main/CHANGELOG.md).

- Upgrade the core runtime and provider package together, then rebuild the consuming project to regenerate its stores and SQL. Do not deploy old generated code with the new runtime.
- User and role SQL now implement `IIdentityUserConcurrencySql` and `IIdentityRoleConcurrencySql`. Token SQL implements `IIdentityUserTokenConcurrencySql`. Legacy SQL is rejected with regeneration guidance rather than silently performing unsafe writes.
- Do not change `ConcurrencyStamp` before calling `UpdateAsync`. The store compares the loaded stamp, rotates it only after success, and returns `ConcurrencyFailure` for stale or deleted rows. Reload before retrying.
- Database `NULL` values must survive materialization, including Identity's constructor-generated stamps. Store queries preserve those nulls without changing global Dapper settings.
- SQL Server database-generated IDs use the column's default, including `NEWSEQUENTIALID()` for GUID schemas. String-key tables need an ID default or `[InsertOwnId]` with an application-supplied ID.
- `[InsertOwnId]` and mapped properties are inherited from application base classes. Unrelated competing Identity entity types produce a diagnostic rather than an arbitrary store.

### Example API limitations

The Web API includes a fixed, publicly known demo signing key, so no environment variable is required to run the example. Do not use that key for a real deployment: anyone with the source can forge tokens. When adapting or deploying the example, override `TokenServiceOptions:SigningKey` using a secret manager, user secrets, or the `TokenServiceOptions__SigningKey` environment variable. Use a private, cryptographically random value, not a password, and never commit it. The application rejects missing, whitespace-only, or shorter-than-32-byte UTF-8 values.

To optionally override the demo key with a temporary development key in PowerShell:

```powershell
$env:TokenServiceOptions__SigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet run --project src/AdaskoTheBeAsT.Identity.Dapper.WebApi/AdaskoTheBeAsT.Identity.Dapper.WebApi.csproj
```

Changing the signing key invalidates existing access tokens. Provision the first `Administrator` through a trusted administrative process; public registration cannot grant that role.

The Web API is a single-process example, not an OAuth authorization server. Refresh tokens are single-use, bound to user ID and security stamp, and stored only in memory. `TokenServiceOptions:RefreshTokenCapacity` defaults to 10,000 outstanding tokens and must be positive. At capacity, issuance evicts the oldest outstanding token; that client must sign in again. Expired and consumed tokens release their slots, and service disposal clears the store. The store does not retain access-token JWTs. Restarting the process invalidates refresh tokens; multiple instances need a shared atomic token store. Already-issued access tokens remain valid until expiration.

`GET /api/Role` now returns a database page, not the entire directory. It defaults to `offset=0&pageSize=100`; page size must be 1–1000. Clients must request subsequent pages to enumerate all roles.

Password sign-in applies Identity's eligibility and lockout checks. Accounts with two-factor authentication enabled cannot use this example's password exchange because it has no second-factor flow. Public registration cannot assign roles; omitted roles in an update leave membership unchanged.

Token requests use form data. Invalid password fields return HTTP 400 with `invalid_request`; unsupported grants, including `client_credentials`, return HTTP 400 with `unsupported_grant_type`.

### Tested configurations

Generator tests compile `string`, `int`, `long`, and GUID keys, both normalized-column modes, application-supplied IDs, inheritance, and custom mappings. Database integration suites execute a key/normalized-column/application-ID configuration matrix across all five providers, including mapped paging and nullable/excluded custom claim fields. Store contract tests cover stale writes, null stamps, exact token comparison, cancellation, and recovery-code races. Oracle additionally exercises inherited dates and custom auxiliary fields. This coverage is not an all-schema database compatibility guarantee.

## 🛠️ Local development

### Prerequisites

- .NET SDK `10.0.401` (pinned in `global.json`)
- Docker Desktop / Docker Engine for integration tests
- PowerShell for `clean.ps1`
- Visual Studio + SSDT if you need to edit SQL Server `.sqlproj` database projects

### String templates

For multiline SQL and source templates, use raw strings (`"""`) and keep the closing delimiter aligned with the content indentation that should be removed. Use `$$"""` for interpolated C# templates: single braces remain literal, and `{{expression}}` inserts a value. If a template contains `"""`, use at least four quotes for its outer delimiter (`$$""""` when interpolating). Simple strings and newline separators can remain ordinary literals.

Generated SQL goes through the shared `RawStringLiteral.Format` helper, which chooses a delimiter longer than any embedded quote sequence and preserves SQL whitespace. Provider SQL builders should return SQL directly, without escaping quotes for C#.

Format SQL clauses on separate lines. Use leading commas for column lists and assignments, aligned just before the first item. Put `INNER JOIN` at the end of the preceding table line and indent the joined table under the first table. Put additional filter predicates on separate `AND` lines.

Place the opening delimiter on the line after `=` in generated SQL properties, with SQL content and the closing delimiter aligned:

```csharp
public string CreateSql { get; } =
    """
    INSERT INTO AspNetRoles([Name])
    OUTPUT inserted.[Id]
    VALUES(@Name);
    """;
```

### Handy commands

Tests use Microsoft.Testing.Platform, selected in `global.json`. Use `--project` and `--report-xunit-trx` rather than VSTest's positional project and `--logger` arguments. Reqnroll code-behind is generated under `obj`; switching framework plugins invalidates its generation cache. Tracked code-behind files remain untouched.

Use AwesomeAssertions for test assertions. xUnit remains the test runner, Reqnroll supplies scenarios, and Verify handles snapshots. Preserve exact exception types with `ThrowExactly` / `ThrowExactlyAsync` and ordered collection comparisons with `Equal` when order is part of the contract.

```bash
# restore everything
dotnet restore AdaskoTheBeAsT.Identity.Dapper.sln

# recommended filtered build loop
dotnet build WithoutSqlDb.slnf

# focused MySQL loop
dotnet build MySQL.slnf

# run the Web API sample
dotnet run --project src/AdaskoTheBeAsT.Identity.Dapper.WebApi/AdaskoTheBeAsT.Identity.Dapper.WebApi.csproj

# run a provider unit snapshot test project
dotnet test --project test/unit/AdaskoTheBeAsT.Identity.Dapper.PostgreSql.Test/AdaskoTheBeAsT.Identity.Dapper.PostgreSql.Test.csproj

# run a provider integration test project (Docker required)
dotnet test --project test/integ/AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest/AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.csproj

# clean bin/obj folders
pwsh ./clean.ps1
```

## 🧭 Repository map

```text
src/
  AdaskoTheBeAsT.Identity.Dapper/            shared runtime abstractions and base store types
  AdaskoTheBeAsT.Identity.Dapper.SqlServer/  SQL Server source generator package
  AdaskoTheBeAsT.Identity.Dapper.PostgreSql/ PostgreSQL source generator package
  AdaskoTheBeAsT.Identity.Dapper.MySql/      MySQL source generator package
  AdaskoTheBeAsT.Identity.Dapper.Oracle/     Oracle source generator package
  AdaskoTheBeAsT.Identity.Dapper.Sqlite/     SQLite source generator package
  AdaskoTheBeAsT.Identity.Dapper.WebApi/     runnable example app

db/        schema scripts and SQL Server schema projects
samples/   consumer samples and provider-specific notes
test/      provider unit snapshot tests and integration tests
```

## 🤝 Contributing

Pull requests are welcome. A good contributor loop is:

1. build the relevant solution filter (`WithoutSqlDb.slnf` or `MySQL.slnf`)
2. run the provider test project you touched
3. keep generated SQL and snapshots aligned with the implementation

## 📄 License

This project is licensed under the [Apache License 2.0](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/blob/main/LICENSE).

## 💬 Support

- Issues: <https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/issues>
- NuGet: <https://www.nuget.org/packages/AdaskoTheBeAsT.Identity.Dapper/>

If this library saves you time, a GitHub star is always appreciated.
