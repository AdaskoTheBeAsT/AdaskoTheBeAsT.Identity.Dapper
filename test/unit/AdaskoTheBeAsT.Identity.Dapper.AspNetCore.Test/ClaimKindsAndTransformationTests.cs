using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using Xunit;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test;

public sealed class ClaimKindsAndTransformationTests
{
    [Fact]
    public async Task LoginEmitsOnlyFrontendClaimsIntoJwtAndEnrichesBackendAndDataClaimsViaTransformationAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var workspaceId = Guid.NewGuid();

        await using var harness = await CreateClaimKindIssuerHarnessAsync();
        var catalog = harness.Services.GetRequiredService<IJwtClaimCatalogStore>();
        await SeedSampleCatalogDefinitionsAsync(catalog, ct);

        var user = await harness.SeedUserAsync("claims-alice", "Strong!Pass123");
        await AssignUserClaimsAsync(harness.Services, user, BuildAliceClaims(workspaceId));

        using var loginResponse = await harness.Client.PostAsJsonAsync(
            "/auth/login",
            new JwtLoginRequest { UserName = "claims-alice", Password = "Strong!Pass123" },
            ct);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);
        tokens.Should().NotBeNull();
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();

        AssertAccessTokenContainsOnlyFrontendClaims(tokens.AccessToken);

        using var req = new HttpRequestMessage(HttpMethod.Get, "/reports/summary");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var reportResp = await harness.Client.SendAsync(req, ct);
        reportResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await reportResp.Content.ReadFromJsonAsync<EnrichedReportSummaryDto>(ct);
        summary.Should().NotBeNull();
        summary.CanApprove.Should().BeTrue();
        summary.DepartmentIds.Should().Equal(10, 20);
        summary.CostCenterIds.Should().Equal(9_000_000_001L);
        summary.WorkspaceIds.Should().Equal(workspaceId);
        summary.Regions.Should().Equal("emea");
        summary.HasInactiveOrUnregistered.Should().BeFalse();
    }

    [Fact]
    public async Task IncludeRolesInAccessTokenOptionControlsRoleClaimEmissionWhenFilteringIsEnabledAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync(
            configureBuilder: jwt => jwt.AddClaimKindSupport(ck =>
            {
                ck.FilterAccessTokenClaimsByKind = true;
                ck.IncludeRolesInAccessToken = false;
            }));

        var catalog = harness.Services.GetRequiredService<IJwtClaimCatalogStore>();
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(
                IdentityClaimKindNames.FrontendClaim,
                "ui:nav:billing",
                ClaimValueTypes.Boolean,
                "Billing Nav",
                null,
                true),
            ct);

        var user = await harness.SeedUserAsync("role-filter-user", "Strong!Pass123");
        using (var scope = harness.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TestUser>>();
            await userManager.AddToRoleAsync(user, "Manager");
            await userManager.AddClaimAsync(user, new Claim("ui:nav:billing", "true"));
        }

        using var loginResponse = await harness.Client.PostAsJsonAsync(
            "/auth/login",
            new JwtLoginRequest { UserName = "role-filter-user", Password = "Strong!Pass123" },
            ct);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);
        tokens.Should().NotBeNull();
        var parsedJwt = new JsonWebTokenHandler().ReadJsonWebToken(tokens.AccessToken);

        parsedJwt.Claims.Should().Contain(c => string.Equals(c.Type, "ui:nav:billing", StringComparison.Ordinal));
        parsedJwt.Claims.Should().NotContain(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.Ordinal));
    }

    [Fact]
    public async Task JwtClaimsTransformationIsIdempotentSkipsUnauthenticatedAndPreservesValueTypesAsync()
    {
        var storeMock = new Mock<IJwtClaimCatalogStore>(MockBehavior.Strict);
        storeMock
            .Setup(s => s.GetUserClaimsForTransformationAsync("user-77", It.IsAny<ISet<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ResolvedClaimRecord("api:orders:write", "true", ClaimValueTypes.Boolean, IdentityClaimKindNames.BackendClaim),
                new ResolvedClaimRecord("data:branch_id", "404", ClaimValueTypes.Integer32, IdentityClaimKindNames.DataClaim),
            ]);

        await using var sp = new ServiceCollection().AddMemoryCache().BuildServiceProvider();
        var monitor = new FixedOptionsMonitor<IdentityJwtValidationOptions>(
            CreateValidationOptionsWithClaimKinds(useDistributedCache: false));
        var cache = new JwtClaimTransformationCache(sp.GetRequiredService<IMemoryCache>(), monitor, sp);
        var transformer = new JwtClaimsTransformation(storeMock.Object, cache, monitor, sp);

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        (await transformer.TransformAsync(anonymous)).Should().BeSameAs(anonymous);
        storeMock.Verify(
            s => s.GetUserClaimsForTransformationAsync(It.IsAny<string>(), It.IsAny<ISet<string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);

        var authenticated = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(IdentityJwtClaimTypes.Subject, "user-77"),
                    new Claim("ui:orders:view", "true", ClaimValueTypes.Boolean),
                ],
                authenticationType: "Bearer"));

        var firstPass = await transformer.TransformAsync(authenticated);
        var secondPass = await transformer.TransformAsync(firstPass);

        secondPass.Should().BeSameAs(firstPass);
        secondPass.HasClaim(IdentityJwtClaimTypes.ClaimsTransformed, "true").Should().BeTrue();
        secondPass.FindAll("api:orders:write").Should().ContainSingle();
        var branchClaim = secondPass.FindFirst("data:branch_id");
        branchClaim.Should().NotBeNull();
        branchClaim.Value.Should().Be("404");
        branchClaim.ValueType.Should().Be(ClaimValueTypes.Integer32);
        secondPass.GetInt32ClaimValues("data:branch_id").Should().Equal(404);

        storeMock.Verify(
            s => s.GetUserClaimsForTransformationAsync("user-77", It.IsAny<ISet<string>?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransformationCacheServesCachedClaimsAndReloadsAfterUserOrGlobalInvalidationAsync(bool useDistributedCache)
    {
        var ct = TestContext.Current.CancellationToken;
        var currentDepartment = "100";
        var storeCalls = 0;

        var storeMock = new Mock<IJwtClaimCatalogStore>(MockBehavior.Strict);
        storeMock
            .Setup(s => s.GetUserClaimsForTransformationAsync("u-cache-1", It.IsAny<ISet<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref storeCalls);
                return
                [
                    new ResolvedClaimRecord("data:department_id", currentDepartment, ClaimValueTypes.Integer32, IdentityClaimKindNames.DataClaim),
                ];
            });

        await using var sp = new ServiceCollection()
            .AddMemoryCache()
            .AddDistributedMemoryCache()
            .BuildServiceProvider();

        var monitor = new FixedOptionsMonitor<IdentityJwtValidationOptions>(
            CreateValidationOptionsWithClaimKinds(useDistributedCache));
        var cache = new JwtClaimTransformationCache(sp.GetRequiredService<IMemoryCache>(), monitor, sp);
        var transformer = new JwtClaimsTransformation(storeMock.Object, cache, monitor, sp);

        (await transformer.TransformAsync(CreateBearerPrincipal("u-cache-1")))
            .GetInt32ClaimValues("data:department_id").Should().Equal(100);
        storeCalls.Should().Be(1);

        currentDepartment = "200";
        (await transformer.TransformAsync(CreateBearerPrincipal("u-cache-1")))
            .GetInt32ClaimValues("data:department_id").Should().Equal(100);
        storeCalls.Should().Be(1);

        await cache.InvalidateUserAsync("u-cache-1", ct);
        (await transformer.TransformAsync(CreateBearerPrincipal("u-cache-1")))
            .GetInt32ClaimValues("data:department_id").Should().Equal(200);
        storeCalls.Should().Be(2);

        currentDepartment = "300";
        await cache.InvalidateAllAsync(ct);
        (await transformer.TransformAsync(CreateBearerPrincipal("u-cache-1")))
            .GetInt32ClaimValues("data:department_id").Should().Equal(300);
        storeCalls.Should().Be(3);
    }

    [Fact]
    public async Task StandaloneResourceApiEnrichesBackendAndDataClaimsWithoutUserManagerAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var frontendJwt = MintStandaloneFrontendJwt(ecdsa.ExportECPrivateKeyPem());

        await using var app = await StartStandaloneEnrichedResourceApiAsync(ecdsa.ExportSubjectPublicKeyInfoPem(), ct);
        app.Services.GetService<UserManager<TestUser>>().Should().BeNull();

        using var client = app.GetTestClient();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/invoices/export");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", frontendJwt.AccessToken);
        using var resp = await client.SendAsync(req, ct);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await resp.Content.ReadAsStringAsync(ct);
        payload.Should().Contain("77");
    }

    [Fact]
    public void ClaimKindValidationOptionsValidatorRejectsBlankKindNamesAndNegativeCacheDuration()
    {
        var validator = new IdentityJwtValidationOptionsValidator();

        var baseValid = CreateValidationOptionsWithClaimKinds(useDistributedCache: false);
        validator.Validate(name: null, baseValid).Succeeded.Should().BeTrue();

        var zeroDurationDisablesCache = CreateValidationOptionsWithClaimKinds(useDistributedCache: false);
        zeroDurationDisablesCache.ClaimKinds.CacheDuration = TimeSpan.Zero;
        validator.Validate(name: null, zeroDurationDisablesCache).Succeeded.Should().BeTrue();

        var negativeDuration = CreateValidationOptionsWithClaimKinds(useDistributedCache: false);
        negativeDuration.ClaimKinds.CacheDuration = TimeSpan.FromSeconds(-1);
        validator.Validate(name: null, negativeDuration).Failed.Should().BeTrue();

        var blankAccessKind = CreateValidationOptionsWithClaimKinds(useDistributedCache: false);
        blankAccessKind.ClaimKinds.AccessTokenClaimKinds.Add("   ");
        validator.Validate(name: null, blankAccessKind).Failed.Should().BeTrue();

        var blankTransformKind = CreateValidationOptionsWithClaimKinds(useDistributedCache: false);
        blankTransformKind.ClaimKinds.TransformationClaimKinds.Add(string.Empty);
        validator.Validate(name: null, blankTransformKind).Failed.Should().BeTrue();
    }

    private static IReadOnlyList<Claim> BuildAliceClaims(Guid workspaceId) =>
    [
        new Claim("ui:reports:view", "true"),
        new Claim("api:reports:approve", "true"),
        new Claim("data:department_id", "10"),
        new Claim("data:department_id", "20"),
        new Claim("data:cost_center_id", "9000000001"),
        new Claim("data:workspace_id", workspaceId.ToString("D")),
        new Claim("data:region", "emea"),
        new Claim("legacy:inactive", "true"),
        new Claim("unregistered:secret", "do-not-expose"),
    ];

    private static void AssertAccessTokenContainsOnlyFrontendClaims(string accessToken)
    {
        var parsedJwt = new JsonWebTokenHandler().ReadJsonWebToken(accessToken);
        parsedJwt.Claims.Should().Contain(c =>
            string.Equals(c.Type, "ui:reports:view", StringComparison.Ordinal) &&
            string.Equals(c.Value, "true", StringComparison.Ordinal));
        parsedJwt.Claims.Should().NotContain(c => string.Equals(c.Type, "api:reports:approve", StringComparison.Ordinal));
        parsedJwt.Claims.Should().NotContain(c => string.Equals(c.Type, "data:department_id", StringComparison.Ordinal));
        parsedJwt.Claims.Should().NotContain(c => string.Equals(c.Type, "data:cost_center_id", StringComparison.Ordinal));
        parsedJwt.Claims.Should().NotContain(c => string.Equals(c.Type, "data:workspace_id", StringComparison.Ordinal));
        parsedJwt.Claims.Should().NotContain(c => string.Equals(c.Type, "legacy:inactive", StringComparison.Ordinal));
        parsedJwt.Claims.Should().NotContain(c => string.Equals(c.Type, "unregistered:secret", StringComparison.Ordinal));
    }

    private static IssuedAccessToken MintStandaloneFrontendJwt(string privatePem)
    {
        var issuerOptions = new IdentityJwtOptions
        {
            Issuer = "https://idp.example.test",
            TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        };
        issuerOptions.Audiences.Add("https://invoices-api.example.test");
        issuerOptions.SigningKeys.Add(new JwtSigningKeyDescriptor
        {
            Kid = "ec-standalone-1",
            Algorithm = JwtKeyAlgorithm.ES256,
            Pem = privatePem,
            IsActiveSigningKey = true,
        });

        using var signer = new DefaultJwtSigningCredentialsProvider(MSOptions.Create(issuerOptions));
        var generator = new JwtTokenGenerator(MSOptions.Create(issuerOptions), signer, new AdjustableTimeProvider());
        return generator.GenerateAccessToken(
            new JwtTokenDescriptorContext("u-standalone", "s-standalone", ["pwd"], [new Claim("ui:invoices:list", "true")]));
    }

    private static async Task<WebApplication> StartStandaloneEnrichedResourceApiAsync(string publicPem, CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddIdentityJwtValidation(v =>
            {
                v.Issuer = "https://idp.example.test";
                v.Audiences.Add("https://invoices-api.example.test");
                v.SigningKeys.Add(new JwtSigningKeyDescriptor
                {
                    Kid = "ec-standalone-1",
                    Algorithm = JwtKeyAlgorithm.ES256,
                    Pem = publicPem,
                    IsActiveSigningKey = false,
                });
            })
            .AddClaimKindTransformation();

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("InvoicesExport", p => p.RequireClaim("api:invoices:export", "true"));

        var app = builder.Build();
        var inMemoryCatalog = (InMemoryJwtClaimCatalogStore)app.Services.GetRequiredService<IJwtClaimCatalogStore>();
        await inMemoryCatalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.BackendClaim, "api:invoices:export", ClaimValueTypes.Boolean, "Export Invoices", null, true),
            ct);
        await inMemoryCatalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.DataClaim, "data:tenant_id", ClaimValueTypes.Integer32, "Tenant Id", null, true),
            ct);
        inMemoryCatalog.SetUserClaims(
            "u-standalone",
            [
                new Claim("api:invoices:export", "true"),
                new Claim("data:tenant_id", "77"),
            ]);

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet(
                "/invoices/export",
                (ClaimsPrincipal principal) => Results.Ok(new { TenantIds = principal.GetInt32ClaimValues("data:tenant_id") }))
            .RequireAuthorization("InvoicesExport");

        await app.StartAsync(ct);
        return app;
    }

    private static Task<JwtTestHostHarness> CreateClaimKindIssuerHarnessAsync() =>
        JwtTestHostHarness.CreateAsync(
            configureBuilder: jwt => jwt.AddClaimKindSupport(),
            configureServices: services =>
            {
                services.AddAuthorizationBuilder()
                    .AddPolicy("ReportsApprove", p => p.RequireClaim("api:reports:approve", "true"));
            },
            configureApp: app =>
            {
                app.MapGet(
                        "/reports/summary",
                        (ClaimsPrincipal user) => Results.Ok(new EnrichedReportSummaryDto(
                            CanApprove: user.HasClaim("api:reports:approve", "true"),
                            DepartmentIds: user.GetInt32ClaimValues("data:department_id").ToArray(),
                            CostCenterIds: user.GetInt64ClaimValues("data:cost_center_id").ToArray(),
                            WorkspaceIds: user.GetGuidClaimValues("data:workspace_id").ToArray(),
                            Regions: user.GetClaimValues("data:region").ToArray(),
                            HasInactiveOrUnregistered: user.HasClaim(c =>
                                string.Equals(c.Type, "legacy:inactive", StringComparison.Ordinal) ||
                                string.Equals(c.Type, "unregistered:secret", StringComparison.Ordinal)))))
                    .RequireAuthorization("ReportsApprove");
            });

    private static async Task SeedSampleCatalogDefinitionsAsync(IJwtClaimCatalogStore catalog, CancellationToken ct)
    {
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.FrontendClaim, "ui:reports:view", ClaimValueTypes.Boolean, "View Reports", null, true),
            ct);
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.BackendClaim, "api:reports:approve", ClaimValueTypes.Boolean, "Approve Reports", null, true),
            ct);
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.DataClaim, "data:department_id", ClaimValueTypes.Integer32, "Department Id", null, true),
            ct);
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.DataClaim, "data:cost_center_id", ClaimValueTypes.Integer64, "Cost Center Id", null, true),
            ct);
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.DataClaim, "data:workspace_id", ClaimValueTypes.String, "Workspace Id", null, true),
            ct);
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.DataClaim, "data:region", ClaimValueTypes.String, "Region", null, true),
            ct);
        await catalog.UpsertClaimDefinitionAsync(
            new ClaimDefinitionUpsertRequest(IdentityClaimKindNames.BackendClaim, "legacy:inactive", ClaimValueTypes.Boolean, "Inactive", null, false),
            ct);
    }

    private static async Task AssignUserClaimsAsync(
        IServiceProvider services,
        TestUser user,
        IReadOnlyList<Claim> claims)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TestUser>>();
        var result = await userManager.AddClaimsAsync(user, claims);
        result.Succeeded.Should().BeTrue();
    }

    private static IdentityJwtValidationOptions CreateValidationOptionsWithClaimKinds(bool useDistributedCache)
    {
        var options = new IdentityJwtValidationOptions
        {
            Issuer = "https://auth.example.test",
        };
        options.Audiences.Add("https://api.example.test");
        options.SigningKeys.Add(new JwtSigningKeyDescriptor
        {
            Kid = "hs-valid",
            Algorithm = JwtKeyAlgorithm.HS256,
            SymmetricKey = Convert.ToBase64String(new byte[32]),
        });
        options.ClaimKinds.EnableClaimsTransformation = true;
        options.ClaimKinds.UseDistributedCache = useDistributedCache;
        return options;
    }

    private static ClaimsPrincipal CreateBearerPrincipal(string subject) =>
        new(
            new ClaimsIdentity(
                [new Claim(IdentityJwtClaimTypes.Subject, subject)],
                authenticationType: "Bearer"));

    private sealed record EnrichedReportSummaryDto(
        bool CanApprove,
        int[] DepartmentIds,
        long[] CostCenterIds,
        Guid[] WorkspaceIds,
        string[] Regions,
        bool HasInactiveOrUnregistered);

    private sealed class FixedOptionsMonitor<TOptions>(TOptions currentValue)
        : Microsoft.Extensions.Options.IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue => currentValue;

        public TOptions Get(string? name) => currentValue;

        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }
}
