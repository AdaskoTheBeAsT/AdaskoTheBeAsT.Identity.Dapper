param(
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path ([IO.Path]::GetTempPath()) ("identity-package-smoke-" + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $scratch 'feed'
$consumer = Join-Path $scratch 'consumer'
$version = '5.0.0-quality-smoke'
$providers = @(
    @{ Name = 'SqlServer'; Driver = 'Microsoft.Data.SqlClient'; DriverVersion = '7.1.1'; ConnectionType = 'Microsoft.Data.SqlClient.SqlConnection' }
    @{ Name = 'PostgreSql'; Driver = 'Npgsql'; DriverVersion = '10.0.3'; ConnectionType = 'Npgsql.NpgsqlConnection' }
    @{ Name = 'MySql'; Driver = 'MySql.Data'; DriverVersion = '26.7.0'; ConnectionType = 'MySql.Data.MySqlClient.MySqlConnection' }
    @{ Name = 'Oracle'; Driver = 'Oracle.ManagedDataAccess.Core'; DriverVersion = '23.26.301'; ConnectionType = 'Oracle.ManagedDataAccess.Client.OracleConnection' }
)

function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}

try {
    New-Item -ItemType Directory -Path $feed, $consumer | Out-Null
    Push-Location $repo
    try {
        foreach ($project in @('AdaskoTheBeAsT.Identity.Dapper', 'AdaskoTheBeAsT.Identity.Dapper.Sqlite') + @($providers | ForEach-Object { "AdaskoTheBeAsT.Identity.Dapper.$($_.Name)" })) {
            Invoke-DotNet pack (Join-Path $repo "src/$project/$project.csproj") -c $Configuration -o $feed "-p:PackageVersion=$version" "-p:GeneratePackageOnBuild=false"
        }
    }
    finally {
        Pop-Location
    }

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.Sqlite" Version="$version" />
    <PackageReference Include="Microsoft.Data.Sqlite.Core" Version="10.0.12" />
    <PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="3.0.5" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $consumer 'Consumer.csproj')

    @'
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.Attributes;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using System.Security.Claims;

namespace Consumer;

internal static class Program
{
    private static async Task Main()
    {
        SQLitePCL.Batteries.Init();
        SqliteDapperConfig.ConfigureTypeHandlers();
        using var keeper = new SqliteConnection("Data Source=package-smoke;Mode=Memory;Cache=Shared");
        await keeper.OpenAsync();
        await keeper.ExecuteAsync("""
            CREATE TABLE AspNetUsers(
                Id TEXT PRIMARY KEY, UserName TEXT, NormalizedUserName TEXT, Email TEXT,
                NormalizedEmail TEXT, EmailConfirmed INTEGER, PasswordHash TEXT,
                SecurityStamp TEXT, ConcurrencyStamp TEXT, PhoneNumber TEXT,
                PhoneNumberConfirmed INTEGER, TwoFactorEnabled INTEGER, LockoutEnd TEXT,
                LockoutEnabled INTEGER, AccessFailedCount INTEGER);
            CREATE TABLE AspNetRoles(
                Id TEXT PRIMARY KEY, Name TEXT, NormalizedName TEXT, ConcurrencyStamp TEXT);
            CREATE TABLE AspNetUserClaims(
                Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId TEXT, ClaimType TEXT, ClaimValue TEXT);
            """);
        using var store = new ApplicationUserOnlyStore(new ConnectionProvider(keeper.ConnectionString));
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "package-consumer", ConcurrencyStamp = null };
        if (!(await store.CreateAsync(user, CancellationToken.None)).Succeeded)
            throw new InvalidOperationException("Packaged store failed to create a user.");
        var found = await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None);
        if (found?.UserName != user.UserName)
            throw new InvalidOperationException("Packaged store failed to read its user.");
        if (found.ConcurrencyStamp != null)
            throw new InvalidOperationException("Packaged store replaced a database-null concurrency stamp.");
        if (!(await store.UpdateAsync(found, CancellationToken.None)).Succeeded)
            throw new InvalidOperationException("Packaged store failed to update its loaded user.");
        var stale = await store.UpdateAsync(user, CancellationToken.None);
        if (stale.Succeeded || !stale.Errors.Any(error => error.Code == "ConcurrencyFailure"))
            throw new InvalidOperationException("Packaged store did not reject a stale update.");
        var page = await ((IPagedUserStore<ApplicationUser>)store).GetUsersPageAsync(0, 1, CancellationToken.None);
        if (page.Count != 1 || page[0].Id != user.Id)
            throw new InvalidOperationException("Packaged store failed database-side user paging.");
        var role = new ApplicationRole { Id = Guid.NewGuid(), Name = "package-role" };
        await keeper.ExecuteAsync("INSERT INTO AspNetRoles(Id, Name) VALUES(@Id, @Name)", role);
        using var roleStore = new ApplicationRoleStore(new ConnectionProvider(keeper.ConnectionString));
        var rolePage = await ((IPagedRoleStore<ApplicationRole>)roleStore).GetRolesPageAsync(0, 1, CancellationToken.None);
        if (rolePage.Count != 1 || rolePage[0].Id != role.Id)
            throw new InvalidOperationException("Packaged store failed database-side role paging.");
        var claims = Enumerable.Range(0, 71).Select(index => new Claim("package-batch", index.ToString())).ToArray();
        await store.AddClaimsAsync(found, claims, CancellationToken.None);
        if ((await store.GetClaimsAsync(found, CancellationToken.None)).Count != claims.Length)
            throw new InvalidOperationException("Packaged store failed batched claim addition.");
        await store.RemoveClaimsAsync(found, claims, CancellationToken.None);
        if ((await store.GetClaimsAsync(found, CancellationToken.None)).Count != 0)
            throw new InvalidOperationException("Packaged store failed batched claim removal.");
        Console.WriteLine("Clean package consumer create/read/null-stamp/concurrency/paging/claim-batch checks passed.");
    }
}

internal sealed class ConnectionProvider(string connectionString) : IIdentityDbConnectionProvider<SqliteConnection>
{
    public SqliteConnection Provide() => new(connectionString);
}

[InsertOwnId]
public class ApplicationUser : IdentityUser<Guid> { }
public class ApplicationRole : IdentityRole<Guid> { }
public class ApplicationUserClaim : IdentityUserClaim<Guid> { }
public class ApplicationRoleClaim : IdentityRoleClaim<Guid> { }
public class ApplicationUserLogin : IdentityUserLogin<Guid> { }
public class ApplicationUserRole : IdentityUserRole<Guid> { }
public class ApplicationUserToken : IdentityUserToken<Guid> { }
'@ | Set-Content -LiteralPath (Join-Path $consumer 'Program.cs')

    $projectFile = Join-Path $consumer 'Consumer.csproj'
    $configFile = Join-Path $scratch 'NuGet.Config'
    @"
<configuration>
  <packageSources>
    <clear />
    <add key="local-smoke" value="$([Security.SecurityElement]::Escape($feed))" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -LiteralPath $configFile
    Invoke-DotNet restore $projectFile --configfile $configFile --packages (Join-Path $scratch 'packages')
    Invoke-DotNet run --project $projectFile --no-restore -c $Configuration

    foreach ($provider in $providers) {
        $providerConsumer = Join-Path $scratch "consumer-$($provider.Name)"
        New-Item -ItemType Directory -Path $providerConsumer | Out-Null
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.$($provider.Name)" Version="$version" />
    <PackageReference Include="$($provider.Driver)" Version="$($provider.DriverVersion)" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $providerConsumer 'Consumer.csproj')

        $oracleProbe = if ($provider.Name -eq 'Oracle') {
            '_ = typeof(Dapper.Oracle.OracleDynamicParameters);'
        }
        else {
            ''
        }
        @'
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.Attributes;
using Microsoft.AspNetCore.Identity;

namespace Consumer;

internal static class Program
{
    private static void Main()
    {
        using var store = new ApplicationUserOnlyStore(new ConnectionProvider());
        __ORACLE_PROBE__
        Console.WriteLine("Packaged __PROVIDER__ store compiled and loaded.");
    }
}

internal sealed class ConnectionProvider : IIdentityDbConnectionProvider<__CONNECTION_TYPE__>
{
    public __CONNECTION_TYPE__ Provide() => throw new NotSupportedException("No database is needed for the package smoke test.");
}

[InsertOwnId]
public class ApplicationUser : IdentityUser<Guid> { }
public class ApplicationRole : IdentityRole<Guid> { }
public class ApplicationUserClaim : IdentityUserClaim<Guid> { }
public class ApplicationRoleClaim : IdentityRoleClaim<Guid> { }
public class ApplicationUserLogin : IdentityUserLogin<Guid> { }
public class ApplicationUserRole : IdentityUserRole<Guid> { }
public class ApplicationUserToken : IdentityUserToken<Guid> { }
'@.Replace('__CONNECTION_TYPE__', $provider.ConnectionType).
    Replace('__ORACLE_PROBE__', $oracleProbe).
    Replace('__PROVIDER__', $provider.Name) |
            Set-Content -LiteralPath (Join-Path $providerConsumer 'Program.cs')

        $providerProject = Join-Path $providerConsumer 'Consumer.csproj'
        Invoke-DotNet restore $providerProject --configfile $configFile --packages (Join-Path $scratch 'packages')
        Invoke-DotNet run --project $providerProject --no-restore -c $Configuration
    }
}
finally {
    # Only this invocation's newly created scratch directory is removed.
    if (Test-Path -LiteralPath $scratch) {
        Remove-Item -LiteralPath $scratch -Recurse -Force
    }
}
