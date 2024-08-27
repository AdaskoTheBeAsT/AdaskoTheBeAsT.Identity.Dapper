# AdaskoTheBeAsT.Identity.Dapper

Custom Dapper implementation for Microsoft.Extensions.Identity.Stores using Source Code Generators.
[https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-custom-storage-providers?view=aspnetcore-7.0](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-custom-storage-providers?view=aspnetcore-7.0).

- It allows to customize classes which are used by Microsoft Identity and generate Dapper code for them.
- Schema of database needs to be created manually but queries are generated automatically.
- User can define own Id type for User and Role.
- User can also skip NormalizedUserName, NormalizedEmail and NormalizedName columns in database and queries.

Sample using nuget within project is available here [Sample](https://github.com/AdaskoTheBeAsT/AdaskoTheBeAsT.Identity.Dapper/tree/main/samples/Sample.SqlServer2).

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


## Usage (version 2.x.x)

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

2. Register stores in your startup file

```csharp
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddRoleStore<ApplicationRoleStore>()
    .AddUserStore<ApplicationUserStore>()
    .AddDefaultTokenProviders();
```

3. Implement `IIdentityDbConnectionProvider<TDbConnection>` interface and register it in your startup file (sample for SqlServer - please adjust for other db providers)

```csharp
public class IdentityDbConnectionProvider
    : IIdentityDbConnectionProvider<SqlConnection>
{
    private readonly IConfiguration _configuration;

    public IdentityDbConnectionProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SqlConnection GetDbConnection()
    {
        return new SqlConnection(_configuration.GetConnectionString("DefaultIdentityConnection"));
    }
}

...

builder.Services.AddSingleton<IIdentityDbConnectionProvider<SqlConnection>, IdentityDbConnectionProvider>();
```

4. based on you database preovider add nuget package and settings:


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

1. Recompile your project

1. You should see generated files in Generated folder (if you set EmitCompilerGenerated files to true)

![Sample output](./doc/output.png)
