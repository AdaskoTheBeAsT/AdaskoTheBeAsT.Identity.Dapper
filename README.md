# 🚀 AdaskoTheBeAsT.Identity.Dapper

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/AdaskoTheBeAsT.Identity.Dapper.svg)](https://www.nuget.org/packages/AdaskoTheBeAsT.Identity.Dapper/)
[![SDK](https://img.shields.io/badge/SDK-.NET%2010.0.201-512BD4.svg)](https://dotnet.microsoft.com/)
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

## ⚡ Quick start

### 1. Install the provider package you need

```bash
# SQL Server
dotnet add package AdaskoTheBeAsT.Identity.Dapper.SqlServer

# PostgreSQL
dotnet add package AdaskoTheBeAsT.Identity.Dapper.PostgreSql

# MySQL
dotnet add package AdaskoTheBeAsT.Identity.Dapper.MySql

# Oracle
dotnet add package AdaskoTheBeAsT.Identity.Dapper.Oracle

# SQLite
dotnet add package AdaskoTheBeAsT.Identity.Dapper.Sqlite
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

## 📚 Examples and useful docs

- [`samples/Sample.SqlServer2`](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/tree/main/samples/Sample.SqlServer2) — SQL Server sample that consumes the NuGet packages
- [`samples/Sample.SqlServer`](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/tree/main/samples/Sample.SqlServer) — SQL Server sample that references local projects
- [`src/AdaskoTheBeAsT.Identity.Dapper.WebApi`](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/tree/main/src/AdaskoTheBeAsT.Identity.Dapper.WebApi) — runnable Web API example
- [`samples/OracleConsoleApp/PrepareOracleDb.txt`](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/blob/main/samples/OracleConsoleApp/PrepareOracleDb.txt) — Oracle prep notes
- [`db/`](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/tree/main/db) — provider schema scripts and SQL Server schema projects

## 🛠️ Local development

### Prerequisites

- .NET SDK `10.0.201` (pinned in `global.json`)
- Docker Desktop / Docker Engine for integration tests
- PowerShell for `clean.ps1`
- Visual Studio + SSDT if you need to edit SQL Server `.sqlproj` database projects

### Handy commands

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
dotnet test test/unit/AdaskoTheBeAsT.Identity.Dapper.PostgreSql.Test/AdaskoTheBeAsT.Identity.Dapper.PostgreSql.Test.csproj

# run a provider integration test project (Docker required)
dotnet test test/integ/AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest/AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.csproj

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

This project is licensed under the [MIT License](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/blob/main/LICENSE).

## 💬 Support

- Issues: <https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/issues>
- NuGet: <https://www.nuget.org/packages/AdaskoTheBeAsT.Identity.Dapper/>

If this library saves you time, a GitHub star is always appreciated.
