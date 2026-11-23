using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Endpoints;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest.Fixtures;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using AwesomeAssertions;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest;

[Collection(SqlServerJwtCollection.Name)]
public sealed class SqlServerClaimCatalogAndTransformationIntegrationTests(SqlServerJwtFixture fixture)
{
    [Fact]
    public async Task CatalogCrudAndUpsertSemanticsWorkAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dboScope = fixture.CreateStoreScope(schemaName: "dbo");
        var catalog = dboScope.ClaimCatalogStore;

        var defaultKinds = await catalog.GetClaimKindsAsync(includeInactive: false, ct);
        var kindNames = defaultKinds.Select(k => k.Name).ToList();
        kindNames.Should().Contain(IdentityClaimKindNames.FrontendClaim);
        kindNames.Should().Contain(IdentityClaimKindNames.BackendClaim);
        kindNames.Should().Contain(IdentityClaimKindNames.DataClaim);

        var customKind = await catalog.UpsertClaimKindAsync(
            new ClaimKindUpsertRequest("AuditClaim", "Audit trail claim kind", IncludeInAccessToken: false, IncludeInTransformation: true, IsActive: true),
            ct);
        customKind.Id.Should().BeGreaterThan(0);

        var updatedCustomKind = await catalog.UpsertClaimKindAsync(
            new ClaimKindUpsertRequest("AuditClaim", "Updated audit description", IncludeInAccessToken: false, IncludeInTransformation: true, IsActive: false),
            ct);
        updatedCustomKind.Id.Should().Be(customKind.Id);
        updatedCustomKind.IsActive.Should().BeFalse();

        (await catalog.GetClaimKindsAsync(includeInactive: false, ct))
            .Should().NotContain(k => string.Equals(k.Name, "AuditClaim", StringComparison.Ordinal));
        (await catalog.GetClaimKindsAsync(includeInactive: true, ct))
            .Should().Contain(k => string.Equals(k.Name, "AuditClaim", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FilteringAndRoleClaimResolutionWorkAcrossDefaultAndCustomSchemasAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var dboScope = fixture.CreateStoreScope(schemaName: "dbo");
        var catalog = dboScope.ClaimCatalogStore;
        var prefix = $"integ_{Guid.NewGuid():N}";

        await SeedPrefixedCatalogAsync(catalog, prefix, ct);

        var filtered = await catalog.FilterAccessTokenClaimsAsync(
            [
                new Claim($"{prefix}:ui:dashboard", "true"),
                new Claim($"{prefix}:api:export", "true"),
                new Claim($"{prefix}:data:dept_id", "42"),
                new Claim($"{prefix}:unregistered", "hidden"),
            ],
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { IdentityClaimKindNames.FrontendClaim },
            ct);

        filtered.Should().ContainSingle();
        filtered[0].Type.Should().Be($"{prefix}:ui:dashboard");
        filtered[0].ValueType.Should().Be(ClaimValueTypes.Boolean);

        var userId = Guid.NewGuid();
        await SeedSqlUserAndRoleClaimsAsync(
            fixture.ConnectionString,
            userId,
            Guid.NewGuid(),
            userClaims: [($"{prefix}:data:dept_id", "101"), ($"{prefix}:data:dept_id", "202")],
            roleClaims: [($"{prefix}:api:export", "true")],
            ct);

        var resolvedDbo = await catalog.GetUserClaimsForTransformationAsync(
            userId.ToString("D"),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                IdentityClaimKindNames.BackendClaim,
                IdentityClaimKindNames.DataClaim,
            },
            ct);

        resolvedDbo.Should().Contain(r =>
            string.Equals(r.ClaimType, $"{prefix}:api:export", StringComparison.Ordinal) &&
            string.Equals(r.ClaimValue, "true", StringComparison.Ordinal));
        resolvedDbo.Where(r => string.Equals(r.ClaimType, $"{prefix}:data:dept_id", StringComparison.Ordinal))
            .Select(r => r.ClaimValue)
            .Should().BeEquivalentTo("101", "202");

        await AssertCrossSchemaResolutionAsync(prefix, userId, ct);
    }

    [Fact]
    public async Task EndToEndSqlServerHostIssuesFrontendOnlyJwtAndEnrichesBackendAndDataClaimsWithInvalidationAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var prefix = $"e2e_{Guid.NewGuid():N}";
        using var host = await BuildSqlClaimKindWebHostAsync(fixture.ConnectionString, prefix, ct);
        using var client = host.GetTestClient();

        using var loginResp = await client.PostAsJsonAsync(
            "/auth/login",
            new JwtLoginRequest { UserName = $"{prefix}_user", Password = "Password123!" },
            ct);
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginPayload = await loginResp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);
        loginPayload.Should().NotBeNull();
        loginPayload.AccessToken.Should().NotBeNullOrWhiteSpace();

        var parsedJwt = new JsonWebTokenHandler().ReadJsonWebToken(loginPayload.AccessToken);
        parsedJwt.Claims.Should().Contain(c => string.Equals(c.Type, $"{prefix}:ui:ledger", StringComparison.Ordinal));
        parsedJwt.Claims.Should().NotContain(c => string.Equals(c.Type, $"{prefix}:api:ledger:post", StringComparison.Ordinal));
        parsedJwt.Claims.Should().NotContain(c => string.Equals(c.Type, $"{prefix}:data:unit_id", StringComparison.Ordinal));

        var initialUnits = await GetLedgerUnitsAsync(client, loginPayload.AccessToken, ct);
        initialUnits.Should().BeEquivalentTo([10, 20]);

        await InsertAdditionalUserClaimAsync(fixture.ConnectionString, Guid.Parse(parsedJwt.Subject), $"{prefix}:data:unit_id", "30", ct);

        var cache = host.Services.GetRequiredService<IJwtClaimTransformationCache>();
        await cache.InvalidateUserAsync(parsedJwt.Subject, ct);

        var updatedUnits = await GetLedgerUnitsAsync(client, loginPayload.AccessToken, ct);
        updatedUnits.Should().BeEquivalentTo([10, 20, 30]);
    }

    [Fact]
    public void SqlServerOptionsValidatorRejectsUnsafeClaimTableIdentifiers()
    {
        var validator = new IdentityJwtSqlServerOptionsValidator();

        var valid = new IdentityJwtSqlServerOptions
        {
            ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=True;",
        };
        validator.Validate(name: null, valid).Succeeded.Should().BeTrue();

        var unsafeClaimKinds = new IdentityJwtSqlServerOptions
        {
            ConnectionString = valid.ConnectionString,
            ClaimKindsTableName = "AspNetClaimKinds;DROP TABLE x;--",
        };
        validator.Validate(name: null, unsafeClaimKinds).Failed.Should().BeTrue();

        var unsafeIdentitySchema = new IdentityJwtSqlServerOptions
        {
            ConnectionString = valid.ConnectionString,
            IdentitySchemaName = "dbo];--",
        };
        validator.Validate(name: null, unsafeIdentitySchema).Failed.Should().BeTrue();
    }

    private static async Task SeedPrefixedCatalogAsync(IJwtClaimCatalogStore catalog, string prefix, CancellationToken ct)
    {
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.FrontendClaim, $"{prefix}:ui:dashboard", ClaimValueTypes.Boolean, "Dashboard UI", null, true),
            ct);
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.BackendClaim, $"{prefix}:api:export", ClaimValueTypes.Boolean, "Export API", null, true),
            ct);
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.DataClaim, $"{prefix}:data:dept_id", ClaimValueTypes.Integer32, "Department Id", null, true),
            ct);
    }

    private static async Task<int[]> GetLedgerUnitsAsync(HttpClient client, string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/sql-ledger");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var resp = await client.SendAsync(req, ct);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<LedgerScopeDto>(ct);
        dto.Should().NotBeNull();
        return dto.UnitIds;
    }

    private static async Task InsertAdditionalUserClaimAsync(
        string connectionString,
        Guid userId,
        string claimType,
        string claimValue,
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO [dbo].[AspNetUserClaims] ([UserId], [ClaimType], [ClaimValue]) VALUES (@UserId, @ClaimType, @ClaimValue);",
                new { UserId = userId, ClaimType = claimType, ClaimValue = claimValue },
                cancellationToken: ct));
    }

    private static async Task SeedSqlUserAndRoleClaimsAsync(
        string connectionString,
        Guid userId,
        Guid roleId,
        IReadOnlyList<(string ClaimType, string ClaimValue)> userClaims,
        IReadOnlyList<(string ClaimType, string ClaimValue)> roleClaims,
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName])
                VALUES (@RoleId, @RoleName, @NormalizedName);
                INSERT INTO [dbo].[AspNetUserRoles] ([UserId], [RoleId])
                VALUES (@UserId, @RoleId);
                """,
                new
                {
                    UserId = userId,
                    RoleId = roleId,
                    RoleName = $"Role_{roleId:N}",
                    NormalizedName = $"ROLE_{roleId:N}",
                },
                cancellationToken: ct));

        foreach (var (claimType, claimValue) in userClaims)
        {
            await conn.ExecuteAsync(
                new CommandDefinition(
                    "INSERT INTO [dbo].[AspNetUserClaims] ([UserId], [ClaimType], [ClaimValue]) VALUES (@UserId, @ClaimType, @ClaimValue);",
                    new { UserId = userId, ClaimType = claimType, ClaimValue = claimValue },
                    cancellationToken: ct));
        }

        foreach (var (claimType, claimValue) in roleClaims)
        {
            await conn.ExecuteAsync(
                new CommandDefinition(
                    "INSERT INTO [dbo].[AspNetRoleClaims] ([RoleId], [ClaimType], [ClaimValue]) VALUES (@RoleId, @ClaimType, @ClaimValue);",
                    new { RoleId = roleId, ClaimType = claimType, ClaimValue = claimValue },
                    cancellationToken: ct));
        }
    }

    private static async Task<IHost> BuildSqlClaimKindWebHostAsync(
        string connectionString,
        string prefix,
        CancellationToken ct)
    {
        var builder = new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(services => ConfigureHostServices(services, connectionString, prefix));
            web.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGroup("/auth").MapIdentityJwtApi<SqlIntegUser>();
                    endpoints.MapGet(
                            "/sql-ledger",
                            (ClaimsPrincipal user) => Results.Ok(new LedgerScopeDto(
                                user.GetInt32ClaimValues($"{prefix}:data:unit_id").ToArray())))
                        .RequireAuthorization("LedgerPost");
                });
            });
        });

        var host = await builder.StartAsync(ct);
        await SeedEndToEndHostDataAsync(host, connectionString, prefix, ct);
        return host;
    }

    private static void ConfigureHostServices(IServiceCollection services, string connectionString, string prefix)
    {
        services.AddRouting();
        services.AddSingleton<IUserStore<SqlIntegUser>, SqlIntegUserStore>();
        services.AddIdentityCore<SqlIntegUser>().AddSignInManager();
        services.AddAuthorizationBuilder()
            .AddPolicy("LedgerPost", p => p.RequireClaim($"{prefix}:api:ledger:post", "true"));
        services.AddDataProtection();
        services.AddIdentityJwtApi<SqlIntegUser>(jwt =>
            {
                jwt.Issuer = "https://sql-ck-web.example.test";
                jwt.Audiences.Add("https://sql-ck-api.example.test");
                jwt.TokenHashKey = Convert.ToBase64String(new byte[32]);
                jwt.SigningKeys.Add(new JwtSigningKeyDescriptor
                {
                    Kid = "ck-hs256",
                    Algorithm = JwtKeyAlgorithm.HS256,
                    SymmetricKey = Convert.ToBase64String(new byte[32]),
                    IsActiveSigningKey = true,
                });
            })
            .AddClaimKindSupport()
            .AddSqlServerJwtStores(sql =>
            {
                sql.ConnectionString = connectionString;
                sql.EnableCleanupHostedService = false;
            });
    }

    private static async Task SeedEndToEndHostDataAsync(
        IHost host,
        string connectionString,
        string prefix,
        CancellationToken ct)
    {
        var catalog = host.Services.GetRequiredService<IJwtClaimCatalogStore>();
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.FrontendClaim, $"{prefix}:ui:ledger", ClaimValueTypes.Boolean, "Ledger UI", null, true),
            ct);
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.BackendClaim, $"{prefix}:api:ledger:post", ClaimValueTypes.Boolean, "Ledger Post", null, true),
            ct);
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.DataClaim, $"{prefix}:data:unit_id", ClaimValueTypes.Integer32, "Unit Id", null, true),
            ct);

        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<SqlIntegUser>>();
        var user = new SqlIntegUser { UserName = $"{prefix}_user" };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddClaimsAsync(
            user,
            [
                new Claim($"{prefix}:ui:ledger", "true"),
                new Claim($"{prefix}:api:ledger:post", "true"),
                new Claim($"{prefix}:data:unit_id", "10"),
                new Claim($"{prefix}:data:unit_id", "20"),
            ]);

        await SeedSqlUserAndRoleClaimsAsync(
            connectionString,
            user.Id,
            Guid.NewGuid(),
            userClaims: [($"{prefix}:data:unit_id", "10"), ($"{prefix}:data:unit_id", "20")],
            roleClaims: [($"{prefix}:api:ledger:post", "true")],
            ct);
    }

    private async Task AssertCrossSchemaResolutionAsync(string prefix, Guid userId, CancellationToken ct)
    {
        await using var customScope = fixture.CreateStoreScope(
            schemaName: SqlServerJwtFixture.CustomSchemaName,
            identitySchemaName: "dbo");
        await customScope.ClaimCatalogStore.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.BackendClaim, $"{prefix}:api:export", ClaimValueTypes.Boolean, "Export API Custom", null, true),
            ct);

        var resolvedCrossSchema = await customScope.ClaimCatalogStore.GetUserClaimsForTransformationAsync(
            userId.ToString("D"),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { IdentityClaimKindNames.BackendClaim },
            ct);
        resolvedCrossSchema.Should().ContainSingle(r =>
            string.Equals(r.ClaimType, $"{prefix}:api:export", StringComparison.Ordinal) &&
            string.Equals(r.ClaimValue, "true", StringComparison.Ordinal));
    }

    private sealed record LedgerScopeDto(int[] UnitIds);
}
