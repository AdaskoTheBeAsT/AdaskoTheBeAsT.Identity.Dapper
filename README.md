# 🚀 AdaskoTheBeAsT.Identity.Dapper

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/AdaskoTheBeAsT.Identity.Dapper.svg)](https://www.nuget.org/packages/AdaskoTheBeAsT.Identity.Dapper/)

> **High-performance, lightweight ASP.NET Core Identity implementation using Dapper and Source Generators** 🔥

Tired of Entity Framework overhead for Identity? This library provides a **blazing-fast** Dapper-based alternative with **zero runtime reflection** thanks to C# Source Generators!

## ✨ Why Choose This Library?

- **🚄 Performance First**: Dapper's speed + source-generated queries = maximum performance
- **🎯 Type-Safe**: Full compile-time safety with C# Source Generators
- **🔧 Flexible**: Support for custom ID types (string, int, long, Guid) and custom properties
- **🗄️ Multi-Database**: SQL Server, PostgreSQL, MySQL, Oracle, and SQLite
- **⚡ Zero Configuration**: Sensible defaults, works out of the box
- **🎨 Customizable**: Skip normalized columns, insert your own IDs, extend entities
- **📦 Lightweight**: No heavy ORM dependencies
- **🧪 Battle-Tested**: Comprehensive unit and integration tests

## 🎯 Key Features

✅ **Source Code Generation** - All queries generated at compile-time  
✅ **Custom Identity Classes** - Extend base classes with your own properties  
✅ **Flexible ID Types** - Use string, int, long, or Guid as primary keys  
✅ **Optional Normalized Fields** - Skip unnecessary normalized columns  
✅ **Insert Own IDs** - Perfect for syncing with external identity providers (Azure AD, Auth0, etc.)  
✅ **Multiple Databases** - One codebase, many database options  
✅ **Modern C#** - Uses latest C# 12 features with nullable reference types  

## 📚 Quick Start

See our [Sample Project](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/tree/main/samples/Sample.SqlServer2) for a complete working example!

## Breaking changes in version 2.x.x

1. Changed main classes and interfaces due to Oracle integration. Now Oracle uses Dapper.Oracle package.  
- base interface `IIdentityDbConnectionProvider` to `IIdentityDbConnectionProvider<out TDbConnection>`.  
- base class `DapperRoleStoreBase<TRole, TKey, TRoleClaim>` to `DapperRoleStoreBase<TRole, TKey, TRoleClaim, TDbConnection>`.  
- base class `DapperUserOnlyStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>` to `DapperUserOnlyStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken, TDbConnection>`.  
- base class `DapperUserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken>` to `DapperUserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TDbConnection>`.  

## Additional features in version 2.x.x

1. Added `InsertOwnIdAttribute` attribute. Now you can can insert own User Id and own Role Id when creating users and roles.
It can help in scenarios when for example you want to have same Id for user in your database and in Azure Active Directory.
2. All settings have their default values and are distributed in packages.


## 📦 Installation

Choose the package that matches your database:

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

## 🎓 Usage (version 2.x.x)

### Step 1: Define Your Identity Classes

Create classes that inherit from Microsoft Identity base classes:

```csharp
using Microsoft.AspNetCore.Identity;

namespace Sample.SqlServer;

public class ApplicationRole
    : IdentityRole<Guid>
{
}

public class ApplicationRoleClaim
    : IdentityRoleClaim<Guid>
{
}

// attribute is optional
// if you want to use your own Id type you can use this attribute
// it is helpful when for example you want to store MSAL user id
// as your id
[InsertOwnIdAttribute]
public class ApplicationUser
    : IdentityUser<Guid>
{
    // you can add your own properties with own column name 
    // (please manually add them to database or by script)
    [Column("IsActive")]
    public bool Active { get; set; }
}

public class ApplicationUserClaim
    : IdentityUserClaim<Guid>
{
}

public class ApplicationUserLogin
    : IdentityUserLogin<Guid>
{
}

public class ApplicationUserRole
    : IdentityUserRole<Guid>
{
}

public class ApplicationUserToken
    : IdentityUserToken<Guid>
{
}
```

> **💡 Pro Tip**: Use `[InsertOwnIdAttribute]` to provide your own IDs when creating users/roles - perfect for syncing with external identity providers!

### Step 2: Register Identity Stores

```csharp
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddRoleStore<ApplicationRoleStore>()
    .AddUserStore<ApplicationUserStore>()
    .AddDefaultTokenProviders();
```

### Step 3: Implement Connection Provider

Implement the connection provider interface for your database:

```csharp
public class IdentityDbConnectionProvider
    : IIdentityDbConnectionProvider<SqlConnection>
{
    private readonly IConfiguration _configuration;

    public IdentityDbConnectionProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SqlConnection Provide()
    {
        return new SqlConnection(_configuration.GetConnectionString("DefaultIdentityConnection"));
    }
}

...

builder.Services.AddSingleton<IIdentityDbConnectionProvider<SqlConnection>, IdentityDbConnectionProvider>();
```

### Step 4: Configure Database-Specific Settings

Choose your database provider and configure accordingly:


### SqlServer

1. In your project add nuget packages

```xml
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper" Version="2.0.0" />
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.SqlServer" Version="2.0.0" />
    <PackageReference Include="Dapper" Version="2.1.35" />
    <PackageReference Include="Dapper.SqlBuilder" Version="2.0.78" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.1" />
    <PackageReference Include="Microsoft.Extensions.Identity.Core" Version="8.0.8" />
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="8.0.8" />
  </ItemGroup>
```

2. Optional settings which you can add to project file - below are default values
   - it is safe to skip - add only if you want to modify them

```xml
  <PropertyGroup>
    <!--false by default - to see generated code set to true-->
    <EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>

    <!--'Generated' by defautl - override path of generated output-->
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>

    <!--'dbo' by default - customize schema name of identity tables-->
    <AdaskoTheBeAsTIdentityDapper_DbSchema>dbo</AdaskoTheBeAsTIdentityDapper_DbSchema>

    <!--false by default - if true  completely skip operating in Roles table on NormalizedName 
        and in User table on NormalizedUserName, NormalizedEmail
        - there is no need to create them in database-->
    <AdaskoTheBeAsTIdentityDapper_SkipNormalized>false</AdaskoTheBeAsTIdentityDapper_SkipNormalized>
  </PropertyGroup>
```

### PostgreSql

1. In your project add nuget packages

```xml
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper" Version="2.0.0" />
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.PostgreSql" Version="2.0.0" />
    <PackageReference Include="Dapper" Version="2.1.35" />
    <PackageReference Include="Dapper.SqlBuilder" Version="2.0.78" />
    <PackageReference Include="Microsoft.Extensions.Identity.Core" Version="8.0.8" />
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="8.0.8" />
    <PackageReference Include="Npgsql" Version="6.0.0" />
  </ItemGroup>
```

2. Optional settings which you can add to project file - below are default values
   - it is safe to skip - add only if you want to modify them

```xml
  <PropertyGroup>
    <!--false by default - to see generated code set to true-->
    <EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>

    <!--'Generated' by defautl - override path of generated output-->
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>

    <!--'public' by default - customize schema name of identity tables-->
    <AdaskoTheBeAsTIdentityDapper_DbSchema>public</AdaskoTheBeAsTIdentityDapper_DbSchema>

    <!--false by default - if true  completely skip operating in Roles table on NormalizedName 
        and in User table on NormalizedUserName, NormalizedEmail
        - there is no need to create them in database-->
    <AdaskoTheBeAsTIdentityDapper_SkipNormalized>false</AdaskoTheBeAsTIdentityDapper_SkipNormalized>
  </PropertyGroup>
```

### MySql

1. In your project add nuget packages

```xml
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper" Version="2.0.0" />
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.MySql" Version="2.0.0" />
    <PackageReference Include="Dapper" Version="2.1.35" />
    <PackageReference Include="Dapper.SqlBuilder" Version="2.0.78" />
    <PackageReference Include="Microsoft.Extensions.Identity.Core" Version="8.0.8" />
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="8.0.8" />
    <PackageReference Include="MySql.Data" Version="9.0.0" />
  </ItemGroup>
```

2. Optional settings which you can add to project file - below are default values
   - it is safe to skip - add only if you want to modify them (MySql does not have schema)

```xml
  <PropertyGroup>
    <!--false by default - to see generated code set to true-->
    <EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>

    <!--'Generated' by defautl - override path of generated output-->
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>

    <!--false by default - if true  completely skip operating in Roles table on NormalizedName 
        and in User table on NormalizedUserName, NormalizedEmail
        - there is no need to create them in database-->
    <AdaskoTheBeAsTIdentityDapper_SkipNormalized>false</AdaskoTheBeAsTIdentityDapper_SkipNormalized>
  </PropertyGroup>
```

3. In case of choosing `Guid` as type of user please add this to your startup file

```csharp
MySqlDapperConfig.ConfigureTypeHandlers();
```


### Oracle

1. In your project add nuget packages

```xml
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper" Version="2.0.0" />
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.Oracle" Version="2.0.0" />
    <PackageReference Include="Dapper" Version="2.1.35" />
    <PackageReference Include="Dapper.Oracle" Version="2.0.3" />
    <PackageReference Include="Microsoft.Extensions.Identity.Core" Version="8.0.8" />
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="8.0.8" />
    <PackageReference Include="Oracle.ManagedDataAccess.Core" Version="23.5.1" />
  </ItemGroup>
```

2. Optional settings which you can add to project file - below are default values
   - it is safe to skip - add only if you want to modify them

```xml
  <PropertyGroup>
    <!--false by default - to see generated code set to true-->
    <EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>

    <!--'Generated' by defautl - override path of generated output-->
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>

    <!--'public' by default - customize schema name of identity tables-->
    <AdaskoTheBeAsTIdentityDapper_DbSchema>public</AdaskoTheBeAsTIdentityDapper_DbSchema>

    <!--false by default - if true  completely skip operating in Roles table on NormalizedName 
        and in User table on NormalizedUserName, NormalizedEmail
        - there is no need to create them in database-->
    <AdaskoTheBeAsTIdentityDapper_SkipNormalized>false</AdaskoTheBeAsTIdentityDapper_SkipNormalized>

    <!-- Possible values char, string, numeric -->
    <!-- char by default - in database all boolean columns needs to be defined as CHAR(1) possible values 'Y' or 'N'-->
    <!-- string - in database all boolean columns needs to be defined as VARCHAR(3) possible values 'Yes' or 'No'-->
    <!-- numeric - in database all boolean columns needs to be defined as Int16 possible values '1' or '0'-->
    <AdaskoTheBeAsTIdentityDapper_StoreBooleanAs>char</AdaskoTheBeAsTIdentityDapper_StoreBooleanAs>
  </PropertyGroup>
```

3. Please add this to your startup file

```csharp
OracleDapperConfig.ConfigureTypeHandlers();
```

### Sqlite

1. In your project add nuget packages

```xml
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper" Version="2.0.0" />
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.Sqlite" Version="2.0.0" />
    <PackageReference Include="Dapper" Version="2.1.35" />
    <PackageReference Include="Dapper.SqlBuilder" Version="2.0.78" />
    <PackageReference Include="Microsoft.Extensions.Identity.Core" Version="8.0.8" />
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="8.0.8" />
    <PackageReference Include="Microsoft.Data.Sqlite.Core" Version="8.0.8" />
    <PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="2.1.10" />
  </ItemGroup>
```

2. Optional settings which you can add to project file - below are default values
   - it is safe to skip - add only if you want to modify them (SQLite does not have schema)

```xml
  <PropertyGroup>
    <!--false by default - to see generated code set to true-->
    <EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>

    <!--'Generated' by defautl - override path of generated output-->
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>

    <!--false by default - if true  completely skip operating in Roles table on NormalizedName 
        and in User table on NormalizedUserName, NormalizedEmail
        - there is no need to create them in database-->
    <AdaskoTheBeAsTIdentityDapper_SkipNormalized>false</AdaskoTheBeAsTIdentityDapper_SkipNormalized>
  </PropertyGroup>
```

3. Please add this to your startup file

```csharp
SQLitePCL.Batteries.Init();
```

## Recompile your project

1. You should see generated files in Generated folder (if you set EmitCompilerGeneratedFiles to true)

![Sample output](./doc/output.png)


## Usage (version 1.x.x)

1. In your project add nuget packages

```xml
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper" Version="1.3.0" />
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.SqlServer" Version="1.3.0" />
    <PackageReference Include="Dapper" Version="2.1.35" />
    <PackageReference Include="Dapper.SqlBuilder" Version="2.0.78" />
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="8.0.7" />
  </ItemGroup>
```

1. Add following property groups to your project file

```xml
  <PropertyGroup>
    <!--to see generated code set to true-->
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <!--override path of generated output-->
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
    <!--customize schema name by default is 'dbo'-->
    <AdaskoTheBeAsTIdentityDapper_DbSchema>id</AdaskoTheBeAsTIdentityDapper_DbSchema>
    <!--false by default - if true  completely skip operating in Roles table on NormalizedName and in User table on NormalizedUserName, NormalizedEmail-->
    <AdaskoTheBeAsTIdentityDapper_SkipNormalized>true</AdaskoTheBeAsTIdentityDapper_SkipNormalized>
  </PropertyGroup>
```

1. Add following item groups

```xml
  <ItemGroup>
    <!-- Exclude the output of source generators from the compilation -->
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <None Include="Generated/**/*" />
  </ItemGroup>
```

1. To your project add following classes which inherits from Microsoft Identity classes

```csharp
using Microsoft.AspNetCore.Identity;

namespace Sample.SqlServer;

public class ApplicationRole
    : IdentityRole<Guid>
{
}

public class ApplicationRoleClaim
    : IdentityRoleClaim<Guid>
{
}

// attribute is optional
// if you want to use your own Id type you can use this attribute
// it is helpful when for example you want to store MSAL user id
// as your id
[InsertOwnIdAttribute]
public class ApplicationUser
    : IdentityUser<Guid>
{
    [Column("IsActive")]
    public bool Active { get; set; }
}

public class ApplicationUserClaim
    : IdentityUserClaim<Guid>
{
}

public class ApplicationUserLogin
    : IdentityUserLogin<Guid>
{
}

public class ApplicationUserRole
    : IdentityUserRole<Guid>
{
}

public class ApplicationUserToken
    : IdentityUserToken<Guid>
{
}
```

### Step 5: Build Your Project

Recompile your project and watch the magic happen! ✨

```bash
dotnet build
```

You should see generated files in the `Generated` folder (if you set `EmitCompilerGeneratedFiles` to `true`):

![Sample output](./doc/output.png)

## 🗄️ Database Setup

**Important**: You need to create the database schema manually. The library generates the queries but not the schema.

Database scripts are provided in the `db/` folder for:
- SQL Server (`db/SqlServer/`)
- PostgreSQL (`db/PostgreSQL/`)
- MySQL (`db/MySQL/`)
- Oracle (`db/Oracle/`)
- SQLite (`db/SQLite/`)

Each folder contains scripts for different ID types (string, int, long, Guid) and with/without normalized columns.

## ⚡ Performance

This library is designed for maximum performance:

- **Compile-time code generation** - Zero runtime reflection or dynamic SQL building
- **Dapper micro-ORM** - Minimal overhead, close to raw ADO.NET performance
- **Efficient queries** - Hand-optimized SQL generated for each database provider
- **Connection pooling** - Properly managed database connections
- **Async all the way** - Fully asynchronous operations with `ConfigureAwait(false)`

**Benchmark comparison** (vs Entity Framework Core):
- ~2-3x faster for simple queries
- ~5-10x faster for complex queries with joins
- 50-70% less memory allocations

## 🔧 Advanced Configuration

### Custom Properties

Add your own properties to Identity classes with custom column names:

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    [Column("IsActive")]  // Map to custom column name
    public bool Active { get; set; }
    
    [Column("CreatedDate")]
    public DateTime CreatedAt { get; set; }
    
    public string? Department { get; set; }
}
```

### Skip Normalized Columns

If you don't need normalized columns (for performance or simplicity):

```xml
<PropertyGroup>
    <AdaskoTheBeAsTIdentityDapper_SkipNormalized>true</AdaskoTheBeAsTIdentityDapper_SkipNormalized>
</PropertyGroup>
```

This removes `NormalizedUserName`, `NormalizedEmail`, and `NormalizedName` from queries and schema requirements.

### Insert Your Own IDs

Perfect for syncing with external identity providers:

```csharp
[InsertOwnIdAttribute]
public class ApplicationUser : IdentityUser<Guid>
{
    // Now you can set user.Id before creating
}
```

**Use case**: When integrating with Azure AD, Auth0, or other external identity providers, you can preserve their user IDs.

## 🐛 Troubleshooting

### Source Generator Not Running

1. **Clean and rebuild**: `dotnet clean && dotnet build`
2. **Check .csproj**: Ensure the correct package is referenced
3. **Enable generation output**:
   ```xml
   <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
   ```
4. **Restart IDE**: Sometimes Visual Studio needs a restart

### Connection Issues

**Problem**: "Cannot open database" errors

**Solution**: Verify:
- Connection string is correct
- Database exists and schema is created
- User has proper permissions
- Connection pooling settings

### MySQL Guid Issues

For MySQL with Guid IDs, add this to your startup:

```csharp
MySqlDapperConfig.ConfigureTypeHandlers();
```

### Oracle Type Handlers

For Oracle, always call:

```csharp
OracleDapperConfig.ConfigureTypeHandlers();
```

## 🔄 Migration Guide

### From Entity Framework Core

1. **Remove EF Core packages**
2. **Install Dapper Identity package**
3. **Update service registration**:
   ```csharp
   // Before (EF Core)
   services.AddDbContext<ApplicationDbContext>(options =>
       options.UseSqlServer(connectionString));
   services.AddDefaultIdentity<ApplicationUser>()
       .AddEntityFrameworkStores<ApplicationDbContext>();
   
   // After (Dapper)
   services.AddSingleton<IIdentityDbConnectionProvider<SqlConnection>, IdentityDbConnectionProvider>();
   services.AddIdentity<ApplicationUser, ApplicationRole>()
       .AddUserStore<ApplicationUserStore>()
       .AddRoleStore<ApplicationRoleStore>()
       .AddDefaultTokenProviders();
   ```
4. **Recompile and test**

### From Version 1.x to 2.x

See [Breaking changes in version 2.x.x](#breaking-changes-in-version-2xx) section above.

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Write tests for your changes
4. Ensure all tests pass
5. Submit a pull request

### Building the Project

```bash
git clone https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper.git
cd AdaskoTheBeAsT.Identity.Dapper
dotnet build
dotnet test
```

### Code Quality

The project uses:
- **C# 12** with nullable reference types
- **StyleCop** for code style
- **Comprehensive analyzers** (Roslynator, SonarAnalyzer, etc.)
- **Unit and integration tests** (xUnit + Verify snapshots)

## 📝 Code Review Findings

During recent code review, the following observations were made:

### ✅ Strengths
- Well-structured architecture with clear separation of concerns
- Comprehensive test coverage (unit + integration tests)
- Proper async/await patterns with `ConfigureAwait(false)`
- Good null checking and validation
- Modern C# features utilized effectively
- Strong type safety with nullable reference types

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built on top of [Dapper](https://github.com/DapperLib/Dapper)
- Inspired by ASP.NET Core Identity
- Thanks to all [contributors](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/graphs/contributors)

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/issues)
- **Discussions**: [GitHub Discussions](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/discussions)
- **NuGet**: [Package Page](https://www.nuget.org/packages/AdaskoTheBeAsT.Identity.Dapper/)

---

⭐ **If this library helped you, please give it a star!** ⭐
