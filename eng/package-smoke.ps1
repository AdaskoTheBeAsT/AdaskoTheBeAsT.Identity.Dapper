param(
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path ([IO.Path]::GetTempPath()) ("identity-package-smoke-" + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $scratch 'feed'
$consumer = Join-Path $scratch 'consumer'
$version = '5.0.0-quality-smoke'
$frameworks = @('net10.0', 'net11.0')
$targetFrameworks = $frameworks -join ';'
$providers = @(
    @{ Name = 'SqlServer'; Driver = 'Microsoft.Data.SqlClient'; DriverVersion = '7.1.1'; ConnectionType = 'Microsoft.Data.SqlClient.SqlConnection' }
    @{ Name = 'PostgreSql'; Driver = 'Npgsql'; DriverVersion = '10.0.3'; ConnectionType = 'Npgsql.NpgsqlConnection' }
    @{ Name = 'MySql'; Driver = 'MySql.Data'; DriverVersion = '26.10.0'; ConnectionType = 'MySql.Data.MySqlClient.MySqlConnection' }
    @{ Name = 'MySqlConnector'; Driver = 'MySqlConnector'; DriverVersion = '2.6.1'; ConnectionType = 'MySqlConnector.MySqlConnection' }
    @{ Name = 'Oracle'; Driver = 'Oracle.ManagedDataAccess.Core'; DriverVersion = '23.26.401'; ConnectionType = 'Oracle.ManagedDataAccess.Client.OracleConnection' }
)
$jwtProviders = @('MySql', 'MySqlConnector', 'Oracle', 'PostgreSql', 'Sqlite')

function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}

function Assert-PackageAssets([string] $Package, [string[]] $ExpectedEntries, [switch] $ExpectJwtSchema) {
    $archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $feed "$Package.$version.nupkg"))
    try {
        $assetEntries = @($archive.Entries.FullName | Where-Object { $_ -match '^(lib|analyzers)/' -and $_ -notmatch '\.(xml|pdb)$' })
        $differences = Compare-Object -ReferenceObject $ExpectedEntries -DifferenceObject $assetEntries -CaseSensitive
        if ($differences) {
            throw "Unexpected lib/analyzer assets in ${Package}: $($differences | Out-String)"
        }
        if ($ExpectJwtSchema -and !$archive.GetEntry('sql/IdentityJwt.sql')) {
            throw "$Package is missing its JWT migration script."
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Restore-Consumer([string] $Project, [string[]] $RuntimePackages) {
    Invoke-DotNet restore $Project --configfile $configFile --packages (Join-Path $scratch 'packages')
    $assetsFile = Join-Path (Split-Path $Project) 'obj/project.assets.json'
    $assets = Get-Content -LiteralPath $assetsFile -Raw | ConvertFrom-Json -AsHashtable
    foreach ($framework in $frameworks) {
        foreach ($package in $RuntimePackages) {
            $compileAssets = $assets.targets[$framework]["$Package/$version"].compile
            $expected = "lib/$framework/$Package.dll"
            if (!$compileAssets -or !$compileAssets.Contains($expected)) {
                throw "$Project did not resolve $expected from the local package."
            }
        }
    }
}

try {
    New-Item -ItemType Directory -Path $feed, $consumer | Out-Null
    '<Project />' | Set-Content -LiteralPath (Join-Path $scratch 'Directory.Build.props')
    '<Project />' | Set-Content -LiteralPath (Join-Path $scratch 'Directory.Build.targets')
    Push-Location $repo
    try {
        $packProjects = @(
            'AdaskoTheBeAsT.Identity.Dapper',
            'AdaskoTheBeAsT.Identity.Dapper.Sqlite',
            'AdaskoTheBeAsT.Identity.Dapper.AspNetCore',
            'AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer'
        ) + @($providers | ForEach-Object { "AdaskoTheBeAsT.Identity.Dapper.$($_.Name)" }) +
            @($jwtProviders | ForEach-Object { "AdaskoTheBeAsT.Identity.Dapper.AspNetCore.$_" })
        foreach ($project in $packProjects) {
            Invoke-DotNet pack (Join-Path $repo "src/$project/$project.csproj") -c $Configuration -o $feed "-p:PackageVersion=$version" "-p:GeneratePackageOnBuild=false"
        }
    }
    finally {
        Pop-Location
    }

    $corePackage = 'AdaskoTheBeAsT.Identity.Dapper'
    $aspNetPackage = "$corePackage.AspNetCore"
    $sqlStoresPackage = "$aspNetPackage.SqlServer"
    Assert-PackageAssets $corePackage (@($frameworks; 'netstandard2.0') | ForEach-Object { "lib/$_/$corePackage.dll" })
    foreach ($package in $aspNetPackage, $sqlStoresPackage) {
        Assert-PackageAssets $package ($frameworks | ForEach-Object { "lib/$_/$package.dll" })
    }
    foreach ($provider in $jwtProviders) {
        $package = "$aspNetPackage.$provider"
        Assert-PackageAssets $package ($frameworks | ForEach-Object { "lib/$_/$package.dll" }) -ExpectJwtSchema
    }
    foreach ($providerName in @('Sqlite') + @($providers.Name)) {
        $package = "$corePackage.$providerName"
        Assert-PackageAssets $package @(
            'lib/netstandard2.0/_._'
            "analyzers/dotnet/cs/$package.dll"
            'analyzers/dotnet/cs/Dapper.dll'
            'analyzers/dotnet/cs/Dapper.SqlBuilder.dll'
            'analyzers/dotnet/cs/Microsoft.Extensions.Identity.Stores.dll'
        )
    }

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>$targetFrameworks</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.Sqlite" Version="$version" />
    <PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="3.0.5" />
  </ItemGroup>
  <ItemGroup Condition="'`$(TargetFramework)' == 'net10.0'">
    <PackageReference Include="Microsoft.Data.Sqlite.Core" Version="10.0.15" />
  </ItemGroup>
  <ItemGroup Condition="'`$(TargetFramework)' == 'net11.0'">
    <PackageReference Include="Microsoft.Data.Sqlite.Core" Version="11.0.0" />
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
    Restore-Consumer $projectFile @($corePackage)
    foreach ($framework in $frameworks) {
        Write-Host "Running SQLite package consumer on $framework."
        Invoke-DotNet run --project $projectFile --no-restore -c $Configuration -f $framework
    }

    foreach ($provider in $providers) {
        $providerConsumer = Join-Path $scratch "consumer-$($provider.Name)"
        New-Item -ItemType Directory -Path $providerConsumer | Out-Null
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>$targetFrameworks</TargetFrameworks>
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
            '_ = typeof(AdaskoTheBeAsT.Dapper.Oracle.OracleDynamicParameters);'
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
        Restore-Consumer $providerProject @($corePackage)
        foreach ($framework in $frameworks) {
            Write-Host "Running $($provider.Name) package consumer on $framework."
            Invoke-DotNet run --project $providerProject --no-restore -c $Configuration -f $framework
        }
    }

    foreach ($provider in $jwtProviders) {
        $providerPackage = "$aspNetPackage.$provider"
        $providerDir = New-Item -ItemType Directory -Path (Join-Path $scratch "jwt-provider-$provider")
        $providerProject = Join-Path $providerDir.FullName 'Consumer.csproj'
        @"
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFrameworks>$targetFrameworks</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="$providerPackage" Version="$version" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $providerProject

        @"
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.$provider.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.$provider.Schema;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddIdentityJwtValidation(_ => { })
    .Add${provider}TokenRevocationStore(o => o.ConnectionString = "not opened by this probe")
    .Add${provider}ClaimCatalogStore(_ => { });
using var host = builder.Build();
if (host.Services.GetRequiredService<ITokenRevocationStore>().GetType().Assembly.GetName().Name != "$providerPackage" ||
    host.Services.GetRequiredService<IJwtClaimCatalogStore>().GetType().Assembly.GetName().Name != "$providerPackage" ||
    !${provider}JwtSchemaHelper.BuildSchemaStatements(new() { TablePrefix = "customjwt" })
        .Any(statement => statement.Contains("customjwt_locks", StringComparison.Ordinal)))
    throw new InvalidOperationException("Packaged $provider runtime registration or schema helper failed.");
Console.WriteLine("Packaged $provider JWT stores and schema helper loaded.");
"@ | Set-Content -LiteralPath (Join-Path $providerDir.FullName 'Program.cs')
        Restore-Consumer $providerProject @($aspNetPackage, $providerPackage)
        foreach ($framework in $frameworks) {
            Write-Host "Running $provider JWT persistence package consumer on $framework."
            Invoke-DotNet run --project $providerProject --no-restore -c $Configuration -f $framework
        }
    }

    $jwtIssuerDir = Join-Path $scratch 'consumer-jwt-issuer'
    $jwtResourceDir = Join-Path $scratch 'consumer-jwt-resource-api'
    New-Item -ItemType Directory -Path $jwtIssuerDir, $jwtResourceDir | Out-Null

    @"
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFrameworks>$targetFrameworks</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.AspNetCore" Version="$version" />
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite" Version="$version" />
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer" Version="$version" />
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.Sqlite" Version="$version" />
    <PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="3.0.5" />
  </ItemGroup>
  <ItemGroup Condition="'`$(TargetFramework)' == 'net10.0'">
    <PackageReference Include="Microsoft.Data.Sqlite.Core" Version="10.0.15" />
  </ItemGroup>
  <ItemGroup Condition="'`$(TargetFramework)' == 'net11.0'">
    <PackageReference Include="Microsoft.Data.Sqlite.Core" Version="11.0.0" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $jwtIssuerDir 'JwtIssuerConsumer.csproj')

    @'
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Endpoints;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.Schema;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Schema;
using AdaskoTheBeAsT.Identity.Dapper.Attributes;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using OtpNet;

namespace JwtIssuerConsumer;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var publicKeyOut = args[0];
        var jwtOut = args[1];

        var ddl = SqlServerJwtSchemaHelper.BuildCreateSchemaAndTablesSql(
            new IdentityJwtSqlServerOptions
            {
                ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=SmokeTest;Trusted_Connection=True;",
                SchemaName = "auth_smoke",
            });
        if (!ddl.Contains("[auth_smoke].[IdentityJwtRefreshTokens]", StringComparison.Ordinal) ||
            !ddl.Contains("[auth_smoke].[AspNetClaimKinds]", StringComparison.Ordinal) ||
            !ddl.Contains("[auth_smoke].[AspNetClaims]", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Packaged SQL Server schema helper did not emit custom schema DDL.");
        }

        var sqlHostBuilder = Host.CreateEmptyApplicationBuilder(null);
        sqlHostBuilder.Services.AddLogging();
        sqlHostBuilder.Services
            .AddIdentityJwtApi<ApplicationUser>(o =>
            {
                o.Issuer = "https://smoke-issuer.example";
                o.Audiences.Add("smoke-api");
                o.TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                using var checkKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                o.SigningKeys.Add(new JwtSigningKeyDescriptor
                {
                    Kid = "check",
                    Algorithm = JwtKeyAlgorithm.ES256,
                    Pem = checkKey.ExportECPrivateKeyPem(),
                    IsActiveSigningKey = true,
                });
            })
            .AddSqlServerJwtStores(o =>
            {
                o.ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=SmokeTest;Trusted_Connection=True;";
                o.EnableCleanupHostedService = false;
            });
        using (var sqlHost = sqlHostBuilder.Build())
        {
            _ = sqlHost.Services.GetRequiredService<AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh.IRefreshTokenStore>();
            _ = sqlHost.Services.GetRequiredService<AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims.IJwtClaimCatalogStore>();
        }

        SQLitePCL.Batteries.Init();
        SqliteDapperConfig.ConfigureTypeHandlers();
        const string connStr = "Data Source=jwt-issuer-smoke;Mode=Memory;Cache=Shared";
        await using var keeper = new SqliteConnection(connStr);
        await keeper.OpenAsync();
        await SqliteJwtSchemaHelper.EnsureSchemaAndTablesAsync(new() { ConnectionString = connStr });
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
            CREATE TABLE AspNetRoleClaims(
                Id INTEGER PRIMARY KEY AUTOINCREMENT, RoleId TEXT, ClaimType TEXT, ClaimValue TEXT);
            CREATE TABLE AspNetUserLogins(
                LoginProvider TEXT, ProviderKey TEXT, ProviderDisplayName TEXT, UserId TEXT,
                PRIMARY KEY(LoginProvider, ProviderKey));
            CREATE TABLE AspNetUserRoles(
                UserId TEXT, RoleId TEXT, PRIMARY KEY(UserId, RoleId));
            CREATE TABLE AspNetUserTokens(
                UserId TEXT, LoginProvider TEXT, Name TEXT, Value TEXT,
                PRIMARY KEY(UserId, LoginProvider, Name));
            """);

        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privatePem = ecdsa.ExportECPrivateKeyPem();
        var publicPem = ecdsa.ExportSubjectPublicKeyInfoPem();
        await File.WriteAllTextAsync(publicKeyOut, publicPem);

        var codeSink = new CapturingCodeSink();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Error);
        builder.Services.AddDataProtection();
        builder.Services.AddSingleton(codeSink);
        builder.Services.AddSingleton<IIdentityDbConnectionProvider<SqliteConnection>>(
            new SqliteConnectionProvider(connStr));
        builder.Services
            .AddIdentityCore<ApplicationUser>(o => o.User.RequireUniqueEmail = true)
            .AddRoles<ApplicationRole>()
            .AddUserStore<ApplicationUserStore>()
            .AddRoleStore<ApplicationRoleStore>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services
            .AddIdentityJwtApi<ApplicationUser>(o =>
            {
                o.Issuer = "https://smoke-issuer.example";
                o.Audiences.Add("smoke-api");
                o.TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                o.SigningKeys.Add(new JwtSigningKeyDescriptor
                {
                    Kid = "smoke-es256-1",
                    Algorithm = JwtKeyAlgorithm.ES256,
                    Pem = privatePem,
                    IsActiveSigningKey = true,
                });
            })
            .AddSqliteJwtStores(o => o.ConnectionString = connStr)
            .AddMfaCodeSender<ApplicationUser, CapturingMfaCodeSender>()
            .AddMfaQrCodeSvg();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGroup("/api/auth").MapIdentityJwtApi<ApplicationUser>(o => o.EnableRegistration = true);
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            var reg = await client.PostAsJsonAsync(
                "/api/auth/register",
                new JwtRegisterRequest
                {
                    UserName = "smokeuser",
                    Email = "smokeuser@example.test",
                    Password = "Strong!Password123",
                });
            if (reg.StatusCode != HttpStatusCode.Created)
            {
                var regErr = await reg.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Expected 201 Created from /register, got {reg.StatusCode}: {regErr}");
            }

            await keeper.ExecuteAsync("UPDATE AspNetUsers SET EmailConfirmed = 1;");

            var wrongPasswordResp = await client.PostAsJsonAsync(
                "/api/auth/login",
                new JwtLoginRequest { Email = "smokeuser@example.test", Password = "Wrong!Password999" });
            if (wrongPasswordResp.StatusCode != HttpStatusCode.Unauthorized ||
                !string.Equals(wrongPasswordResp.Content.Headers.ContentType?.MediaType, "application/problem+json", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected 401 application/problem+json for bad credentials, got {wrongPasswordResp.StatusCode} ({wrongPasswordResp.Content.Headers.ContentType}).");
            }

            var firstLoginResp = await client.PostAsJsonAsync(
                "/api/auth/login",
                new JwtLoginRequest { Email = "smokeuser@example.test", Password = "Strong!Password123" });
            if (!firstLoginResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Initial /login failed ({firstLoginResp.StatusCode}): {await firstLoginResp.Content.ReadAsStringAsync()}");
            }

            var firstLogin = await firstLoginResp.Content.ReadFromJsonAsync<JwtLoginResponse>();
            if (string.IsNullOrWhiteSpace(firstLogin?.AccessToken))
            {
                throw new InvalidOperationException("Initial login did not return an access token.");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstLogin.AccessToken);
            var startMfaResp = await client.PostAsJsonAsync(
                "/api/auth/mfa/methods",
                new JwtMfaMethodUpdateRequest { Method = "email", Enabled = true });
            if (!startMfaResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Start /mfa/methods failed ({startMfaResp.StatusCode}): {await startMfaResp.Content.ReadAsStringAsync()}");
            }

            var startEmailMfa = await startMfaResp.Content.ReadFromJsonAsync<JwtMfaMethodUpdateResponse>();
            if (startEmailMfa is not { VerificationRequired: true } || string.IsNullOrWhiteSpace(startEmailMfa.ChallengeToken))
            {
                throw new InvalidOperationException("Enabling email MFA did not return a verification challenge.");
            }

            var confirmEmailMfa = await client.PostAsJsonAsync(
                "/api/auth/mfa/methods",
                new JwtMfaMethodUpdateRequest
                {
                    Method = "email",
                    Enabled = true,
                    ChallengeToken = startEmailMfa.ChallengeToken,
                    VerificationCode = codeSink.LastEmailCode,
                });
            if (!confirmEmailMfa.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Confirm /mfa/methods failed ({confirmEmailMfa.StatusCode}): {await confirmEmailMfa.Content.ReadAsStringAsync()}");
            }

            client.DefaultRequestHeaders.Authorization = null;
            var mfaLoginResp = await client.PostAsJsonAsync(
                "/api/auth/login",
                new JwtLoginRequest { Email = "smokeuser@example.test", Password = "Strong!Password123" });
            if (!mfaLoginResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Second /login failed ({mfaLoginResp.StatusCode}): {await mfaLoginResp.Content.ReadAsStringAsync()}");
            }

            var mfaChallenge = await mfaLoginResp.Content.ReadFromJsonAsync<JwtLoginResponse>();
            if (mfaChallenge is not { TwoFactorRequired: true } || string.IsNullOrWhiteSpace(mfaChallenge.MfaChallengeToken))
            {
                throw new InvalidOperationException("MFA-enabled login did not require second factor.");
            }

            var verifyResp = await client.PostAsJsonAsync(
                "/api/auth/login/mfa",
                new JwtMfaVerifyRequest
                {
                    MfaChallengeToken = mfaChallenge.MfaChallengeToken,
                    Method = "email",
                    Code = codeSink.LastEmailCode,
                });
            if (!verifyResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"/login/mfa failed ({verifyResp.StatusCode}): {await verifyResp.Content.ReadAsStringAsync()}");
            }

            var verified = await verifyResp.Content.ReadFromJsonAsync<JwtLoginResponse>();
            if (string.IsNullOrWhiteSpace(verified?.AccessToken))
            {
                throw new InvalidOperationException("MFA verification did not issue an access token.");
            }

            // Confirming an authenticator changes the security stamp, so rotate before enrollment.
            var refresh = new JwtRefreshRequest { RefreshToken = verified.RefreshToken ?? throw new InvalidOperationException("Missing refresh token.") };
            using var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", refresh);
            if (!refreshResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"/refresh failed ({refreshResponse.StatusCode}): {await refreshResponse.Content.ReadAsStringAsync()}");
            }

            var rotated = await refreshResponse.Content.ReadFromJsonAsync<JwtLoginResponse>();
            if (string.IsNullOrEmpty(rotated?.AccessToken) || string.IsNullOrEmpty(rotated.RefreshToken) ||
                rotated.RefreshToken == refresh.RefreshToken || rotated.SessionId != verified.SessionId)
            {
                throw new InvalidOperationException("Refresh rotation did not keep the session and issue a new token.");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rotated.AccessToken);
            using var setupResp = await client.PostAsJsonAsync(
                "/api/auth/mfa/authenticator/setup", new JwtMfaAuthenticatorSetupRequest());
            setupResp.EnsureSuccessStatusCode();
            var setup = await setupResp.Content.ReadFromJsonAsync<JwtMfaAuthenticatorSetupResponse>();
            if (setup?.QrCodeSvg?.StartsWith("<svg ", StringComparison.Ordinal) != true ||
                !setup.AuthenticatorUri.StartsWith("otpauth://totp/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Authenticator setup did not return an enrollment URI and SVG.");
            }

            var key = Base32Encoding.ToBytes(setup.SharedKey.Replace(" ", string.Empty, StringComparison.Ordinal));
            var code = new Totp(key).ComputeTotp();
            using var confirmResp = await client.PostAsJsonAsync(
                "/api/auth/mfa/authenticator/confirm", new JwtMfaAuthenticatorConfirmRequest { Code = code });
            confirmResp.EnsureSuccessStatusCode();
            var confirmed = await confirmResp.Content.ReadFromJsonAsync<JwtMfaAuthenticatorConfirmResponse>();
            if (confirmed is not { TwoFactorEnabled: true })
            {
                throw new InvalidOperationException("Authenticator confirmation did not enable TOTP.");
            }

            client.DefaultRequestHeaders.Authorization = null;
            using var replay = await client.PostAsJsonAsync("/api/auth/refresh", refresh);
            using var afterReplay = await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new JwtRefreshRequest { RefreshToken = rotated.RefreshToken });
            if (replay.StatusCode != HttpStatusCode.Unauthorized ||
                afterReplay.StatusCode != HttpStatusCode.Unauthorized ||
                !(await replay.Content.ReadAsStringAsync()).Contains("Refresh token replay detected", StringComparison.Ordinal) ||
                !(await afterReplay.Content.ReadAsStringAsync()).Contains("Refresh token is expired or revoked", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A replayed refresh token did not revoke the replacement.");
            }

            await File.WriteAllTextAsync(jwtOut, verified.AccessToken);
            Console.WriteLine("Packaged SQLite-backed JWT issuer, refresh replay, email and authenticator MFA, QR SVG, and SQL Server store registration checks passed.");
        }
        finally
        {
            await app.StopAsync();
        }
    }
}

internal sealed class CapturingCodeSink
{
    public string LastEmailCode { get; set; } = string.Empty;
}

internal sealed class CapturingMfaCodeSender(CapturingCodeSink sink) : IMfaCodeSender<ApplicationUser>
{
    public Task SendEmailCodeAsync(ApplicationUser user, string email, string code, CancellationToken cancellationToken)
    {
        sink.LastEmailCode = code;
        return Task.CompletedTask;
    }

    public Task SendSmsCodeAsync(ApplicationUser user, string phoneNumber, string code, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task SendSetupLinkAsync(ApplicationUser user, string destination, Uri setupUri, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

internal sealed class SqliteConnectionProvider(string connectionString) : IIdentityDbConnectionProvider<SqliteConnection>
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
'@ | Set-Content -LiteralPath (Join-Path $jwtIssuerDir 'Program.cs')

    $jwtIssuerProject = Join-Path $jwtIssuerDir 'JwtIssuerConsumer.csproj'
    Restore-Consumer $jwtIssuerProject @($corePackage, $aspNetPackage, $sqlStoresPackage, "$aspNetPackage.Sqlite")

    @"
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFrameworks>$targetFrameworks</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="AdaskoTheBeAsT.Identity.Dapper.AspNetCore" Version="$version" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $jwtResourceDir 'JwtResourceApiConsumer.csproj')

    @'
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;

namespace JwtResourceApiConsumer;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var publicPem = await File.ReadAllTextAsync(args[0]);
        var rawJwt = (await File.ReadAllTextAsync(args[1])).Trim();

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddIdentityJwtValidation(o =>
        {
            o.Issuer = "https://smoke-issuer.example";
            o.Audiences.Add("smoke-api");
            o.SigningKeys.Add(new JwtSigningKeyDescriptor
            {
                Kid = "smoke-es256-1",
                Algorithm = JwtKeyAlgorithm.ES256,
                Pem = publicPem,
                IsActiveSigningKey = false,
            });
        }).AddClaimKindTransformation();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet(
            "/resource",
            (ClaimsPrincipal principal) => Results.Ok(new
            {
                Sub = principal.FindFirstValue("sub"),
                Amr = principal.FindAll("amr").Select(c => c.Value).ToArray(),
            }))
            .RequireAuthorization();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            var unauth = await client.GetAsync("/resource");
            if (unauth.StatusCode != HttpStatusCode.Unauthorized ||
                !string.Equals(unauth.Content.Headers.ContentType?.MediaType, "application/problem+json", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected 401 application/problem+json without token, got {unauth.StatusCode} ({unauth.Content.Headers.ContentType}).");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rawJwt);
            var ok = await client.GetAsync("/resource");
            ok.EnsureSuccessStatusCode();
            var body = await ok.Content.ReadAsStringAsync();
            if (!body.Contains("\"mfa\"", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Resource API principal did not preserve MFA amr claim: {body}");
            }

            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(rawJwt);
            var revocationStore = app.Services.GetRequiredService<ITokenRevocationStore>();
            await revocationStore.RevokeAsync(
                new TokenRevocationEntry(
                    Issuer: "https://smoke-issuer.example",
                    SubjectType: RevocationSubjectType.Jti,
                    SubjectKey: parsed.Id,
                    RevokedAtUtc: DateTimeOffset.UtcNow,
                    ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
                    Reason: "smoke-revocation"),
                CancellationToken.None);

            var afterRevoke = await client.GetAsync("/resource");
            if (afterRevoke.StatusCode != HttpStatusCode.Unauthorized ||
                !string.Equals(afterRevoke.Content.Headers.ContentType?.MediaType, "application/problem+json", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected 401 application/problem+json after jti revocation, got {afterRevoke.StatusCode} ({afterRevoke.Content.Headers.ContentType}).");
            }

            Console.WriteLine("Packaged standalone zero-Identity resource API validator checks passed.");
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
'@ | Set-Content -LiteralPath (Join-Path $jwtResourceDir 'Program.cs')

    $jwtResourceProject = Join-Path $jwtResourceDir 'JwtResourceApiConsumer.csproj'
    Restore-Consumer $jwtResourceProject @($corePackage, $aspNetPackage)
    foreach ($issuerFramework in $frameworks) {
        $resourceFramework = $frameworks | Where-Object { $_ -ne $issuerFramework }
        $publicKeyFile = Join-Path $scratch "$issuerFramework-es256-pub.pem"
        $issuedJwtFile = Join-Path $scratch "$issuerFramework-issued-mfa.jwt"
        Write-Host "Running JWT package consumers with $issuerFramework issuing and $resourceFramework validating."
        Invoke-DotNet run --project $jwtIssuerProject --no-restore -c $Configuration -f $issuerFramework -- $publicKeyFile $issuedJwtFile
        Invoke-DotNet run --project $jwtResourceProject --no-restore -c $Configuration -f $resourceFramework -- $publicKeyFile $issuedJwtFile
    }
}
finally {
    # Only this invocation's newly created scratch directory is removed.
    if (Test-Path -LiteralPath $scratch) {
        Remove-Item -LiteralPath $scratch -Recurse -Force
    }
}
