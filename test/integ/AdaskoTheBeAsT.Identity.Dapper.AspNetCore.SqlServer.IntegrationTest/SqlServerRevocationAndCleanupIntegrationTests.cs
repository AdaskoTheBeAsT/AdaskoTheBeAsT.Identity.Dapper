using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Endpoints;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest.Fixtures;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest;

[Collection(SqlServerJwtCollection.Name)]
public sealed class SqlServerRevocationAndCleanupIntegrationTests(SqlServerJwtFixture fixture)
{
    [Fact]
    public async Task RevocationStoreIdempotentMergeAndCutoffSemanticsWorkAcrossScopesAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 11, 10, 16, 0, 0, TimeSpan.Zero);
        await using var scope = fixture.CreateStoreScope(new FakeTimeProvider(now));

        const string issuer = "https://sql-integ.example.test";
        var jti = $"jti-{Guid.NewGuid():N}";
        var sid = $"sid-{Guid.NewGuid():N}";
        var uid = $"uid-{Guid.NewGuid():N}";

        await scope.RevocationStore.RevokeAsync(
            new TokenRevocationEntry(issuer, RevocationSubjectType.Jti, jti, now, now.AddMinutes(5), null, "logout"),
            ct);
        await scope.RevocationStore.RevokeAsync(
            new TokenRevocationEntry(issuer, RevocationSubjectType.Jti, jti, now, now.AddMinutes(15), null, "extended"),
            ct);

        (await scope.RevocationStore.IsRevokedAsync(new TokenRevocationCheckContext(issuer, jti, null, null, now), ct))
            .Should().BeTrue();

        await scope.RevocationStore.RevokeAsync(
            new TokenRevocationEntry(issuer, RevocationSubjectType.SessionId, sid, now, now.AddMinutes(15), null, "replay"),
            ct);
        (await scope.RevocationStore.IsRevokedAsync(new TokenRevocationCheckContext(issuer, "other-jti", sid, null, now), ct))
            .Should().BeTrue();

        var cutoff = now.AddMinutes(10);
        await scope.RevocationStore.RevokeAsync(
            new TokenRevocationEntry(issuer, RevocationSubjectType.UserId, uid, now, now.AddHours(1), cutoff, "compromise"),
            ct);

        (await scope.RevocationStore.IsRevokedAsync(new TokenRevocationCheckContext(issuer, "j1", "s1", uid, cutoff.AddSeconds(-1)), ct))
            .Should().BeTrue();
        (await scope.RevocationStore.IsRevokedAsync(new TokenRevocationCheckContext(issuer, "j2", "s2", uid, cutoff.AddSeconds(5)), ct))
            .Should().BeFalse();
    }

    [Fact]
    public async Task CleanupHostedServicePrunesExpiredRowsAcrossAllSqlTablesPreservingActiveRowsAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 11, 10, 17, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(now);
        await using var scope = fixture.CreateStoreScope(clock, cleanupBatchSize: 50);

        var expiredRefresh = SqlHashHelper.Sha256Hex($"exp-rt-{Guid.NewGuid():N}");
        var activeRefresh = SqlHashHelper.Sha256Hex($"act-rt-{Guid.NewGuid():N}");
        await scope.RefreshStore.StoreAsync(
            new RefreshTokenRecord(Guid.NewGuid(), expiredRefresh, "v1", "s-exp", "u1", "st", "pwd", now.AddHours(-2), now.AddMinutes(-5), now.AddDays(1)),
            ct);
        await scope.RefreshStore.StoreAsync(
            new RefreshTokenRecord(Guid.NewGuid(), activeRefresh, "v1", "s-act", "u1", "st", "pwd", now, now.AddHours(2), now.AddDays(1)),
            ct);

        var expiredMfa = SqlHashHelper.Sha256Hex($"exp-mfa-{Guid.NewGuid():N}");
        var activeMfa = SqlHashHelper.Sha256Hex($"act-mfa-{Guid.NewGuid():N}");
        await scope.MfaStore.StoreAsync(
            new MfaChallengeRecord(Guid.NewGuid(), expiredMfa, "v1", MfaChallengePurpose.LoginChallenge, "u1", "st", "Email", null, null, 0, 5, 0, null, now.AddMinutes(-10), now.AddMinutes(-1)),
            ct);
        await scope.MfaStore.StoreAsync(
            new MfaChallengeRecord(Guid.NewGuid(), activeMfa, "v1", MfaChallengePurpose.LoginChallenge, "u1", "st", "Email", null, null, 0, 5, 0, null, now, now.AddMinutes(10)),
            ct);

        var expiredJti = $"exp-jti-{Guid.NewGuid():N}";
        var activeJti = $"act-jti-{Guid.NewGuid():N}";
        await scope.RevocationStore.RevokeAsync(
            new TokenRevocationEntry("https://sql-integ.example.test", RevocationSubjectType.Jti, expiredJti, now.AddMinutes(-20), now.AddMinutes(-1)),
            ct);
        await scope.RevocationStore.RevokeAsync(
            new TokenRevocationEntry("https://sql-integ.example.test", RevocationSubjectType.Jti, activeJti, now, now.AddMinutes(20)),
            ct);

        await scope.CleanupService.RunCleanupCycleAsync(ct);

        (await scope.RefreshStore.FindByHashAsync(expiredRefresh, ct)).Should().BeNull();
        (await scope.RefreshStore.FindByHashAsync(activeRefresh, ct)).Should().NotBeNull();
        (await scope.MfaStore.FindAsync(expiredMfa, now.AddHours(-1), ct)).Should().BeNull();
        (await scope.MfaStore.FindAsync(activeMfa, now, ct)).Should().NotBeNull();
        (await scope.RevocationStore.IsRevokedAsync(new TokenRevocationCheckContext("https://sql-integ.example.test", activeJti, null, null, now), ct))
            .Should().BeTrue();
    }

    [Fact]
    public async Task MinimalApiWithSqlServerStoresCascadesRefreshReplayIntoImmediateBearerRevocationAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = await BuildSqlBackedWebHostAsync(fixture.ConnectionString, ct);
        using var client = host.GetTestClient();

        using var loginResp = await client.PostAsJsonAsync(
            "/auth/login",
            new JwtLoginRequest { UserName = "sql-user", Password = "Password123!" },
            ct);
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var initial = await loginResp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);

        using var rotateResp = await client.PostAsJsonAsync(
            "/auth/refresh",
            new JwtRefreshRequest { RefreshToken = initial!.RefreshToken! },
            ct);
        rotateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await rotateResp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);

        using var replayResp = await client.PostAsJsonAsync(
            "/auth/refresh",
            new JwtRefreshRequest { RefreshToken = initial.RefreshToken! },
            ct);
        replayResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var bearerReq = new HttpRequestMessage(HttpMethod.Get, "/sql-protected");
        bearerReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rotated!.AccessToken);
        using var protectedResp = await client.SendAsync(bearerReq, ct);
        protectedResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<IHost> BuildSqlBackedWebHostAsync(string connectionString, CancellationToken ct)
    {
        var builder = new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddSingleton<IUserStore<SqlIntegUser>, SqlIntegUserStore>();
                services.AddIdentityCore<SqlIntegUser>().AddSignInManager();
                services.AddAuthorization();
                services.AddDataProtection();
                services.AddIdentityJwtApi<SqlIntegUser>(jwt =>
                {
                    jwt.Issuer = "https://sql-web.example.test";
                    jwt.Audiences.Add("https://sql-web-api.example.test");
                    jwt.TokenHashKey = Convert.ToBase64String(new byte[32]);
                    jwt.SigningKeys.Add(new JwtSigningKeyDescriptor
                    {
                        Kid = "web-hs256",
                        Algorithm = JwtKeyAlgorithm.HS256,
                        SymmetricKey = Convert.ToBase64String(new byte[32]),
                        IsActiveSigningKey = true,
                    });
                }).AddSqlServerJwtStores(sql =>
                {
                    sql.ConnectionString = connectionString;
                    sql.EnableCleanupHostedService = false;
                });
            });
            web.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGroup("/auth").MapIdentityJwtApi<SqlIntegUser>();
                    endpoints.MapGet("/sql-protected", () => Results.Ok()).RequireAuthorization();
                });
            });
        });

        var host = await builder.StartAsync(ct);
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<SqlIntegUser>>();
        await userManager.CreateAsync(new SqlIntegUser { UserName = "sql-user" }, "Password123!");
        return host;
    }
}
