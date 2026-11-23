using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test;

public sealed class MinimalApiLifecycleAndRevocationTests
{
    [Fact]
    public async Task PasswordLoginIssuesJwtAndRefreshPreservesSubAcrossRenameAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync();
        var user = await harness.SeedUserAsync("alice", "Secret123!");

        var tokens = await LoginAsync(harness.Client, "alice", "Secret123!", ct);
        tokens.TwoFactorRequired.Should().BeFalse();
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();

        await harness.UserStore.SetUserNameAsync(user, "alice-renamed", ct);
        await harness.UserStore.SetNormalizedUserNameAsync(user, "ALICE-RENAMED", ct);

        using var refreshResp = await harness.Client.PostAsJsonAsync(
            "/auth/refresh",
            new JwtRefreshRequest { RefreshToken = tokens.RefreshToken },
            ct);
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await refreshResp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);

        var parsed = new JsonWebTokenHandler().ReadJsonWebToken(rotated!.AccessToken);
        parsed.Subject.Should().Be(user.Id.ToString("D"));
        parsed.GetPayloadValue<string>("unique_name").Should().Be("alice-renamed");
        parsed.Claims.Where(c => string.Equals(c.Type, "amr", StringComparison.Ordinal)).Select(c => c.Value).Should().Equal("pwd");
        rotated.SessionId.Should().Be(tokens.SessionId);
    }

    [Fact]
    public async Task RefreshRejectsStampChangedLockedOutAndDeletedUsersAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync();
        var userA = await harness.SeedUserAsync("stamp-user", "Secret123!");
        var userB = await harness.SeedUserAsync("locked-user", "Secret123!");
        var userC = await harness.SeedUserAsync("deleted-user", "Secret123!");

        var loginA = await LoginAsync(harness.Client, "stamp-user", "Secret123!", ct);
        var loginB = await LoginAsync(harness.Client, "locked-user", "Secret123!", ct);
        var loginC = await LoginAsync(harness.Client, "deleted-user", "Secret123!", ct);

        using (var scope = harness.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<TestUser>>();
            await manager.UpdateSecurityStampAsync(userA);
            await manager.SetLockoutEndDateAsync(userB, harness.TimeProvider.GetUtcNow().AddHours(1));
            await manager.DeleteAsync(userC);
        }

        (await PostJsonStatusAsync(harness.Client, "/auth/refresh", new JwtRefreshRequest { RefreshToken = loginA.RefreshToken! }, ct))
            .Should().Be(HttpStatusCode.Unauthorized);
        (await PostJsonStatusAsync(harness.Client, "/auth/refresh", new JwtRefreshRequest { RefreshToken = loginB.RefreshToken! }, ct))
            .Should().Be(HttpStatusCode.Unauthorized);
        (await PostJsonStatusAsync(harness.Client, "/auth/refresh", new JwtRefreshRequest { RefreshToken = loginC.RefreshToken! }, ct))
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SixteenWayParallelRefreshHasSingleWinnerAndReplayRevokesSessionAccessTokenAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync();
        await harness.SeedUserAsync("race-user", "Secret123!");
        var initial = await LoginAsync(harness.Client, "race-user", "Secret123!", ct);

        var refreshTasks = Enumerable.Range(0, 16)
            .Select(async _ => await harness.Client.PostAsJsonAsync(
                "/auth/refresh",
                new JwtRefreshRequest { RefreshToken = initial.RefreshToken! },
                ct))
            .ToArray();
        var responses = await Task.WhenAll(refreshTasks);

        try
        {
            var okResponses = responses.Where(r => r.StatusCode == HttpStatusCode.OK).ToArray();
            responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized).Should().Be(15);
            okResponses.Should().ContainSingle();

            var winnerPayload = await okResponses[0].Content.ReadFromJsonAsync<JwtLoginResponse>(ct);
            (await PostJsonStatusAsync(harness.Client, "/auth/refresh", new JwtRefreshRequest { RefreshToken = initial.RefreshToken! }, ct))
                .Should().Be(HttpStatusCode.Unauthorized);

            (await GetBearerStatusAsync(harness.Client, "/resource/protected", winnerPayload!.AccessToken!, ct))
                .Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            foreach (var r in responses)
            {
                r.Dispose();
            }
        }
    }

    [Fact]
    public async Task RevokeEndpointHandlesTokenSessionAndSameSecondAllSessionsReloginAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync();
        await harness.SeedUserAsync("revoke-user", "Secret123!");

        var s1 = await LoginAsync(harness.Client, "revoke-user", "Secret123!", ct);
        var s2 = await LoginAsync(harness.Client, "revoke-user", "Secret123!", ct);

        (await PostBearerStatusAsync(harness.Client, "/auth/revoke", s1.AccessToken!, new JwtRevokeRequest { Scope = "token" }, ct))
            .Should().Be(HttpStatusCode.NoContent);

        (await GetBearerStatusAsync(harness.Client, "/resource/protected", s1.AccessToken!, ct)).Should().Be(HttpStatusCode.Unauthorized);
        (await GetBearerStatusAsync(harness.Client, "/resource/protected", s2.AccessToken!, ct)).Should().Be(HttpStatusCode.OK);

        using var s1Refreshed = await harness.Client.PostAsJsonAsync(
            "/auth/refresh",
            new JwtRefreshRequest { RefreshToken = s1.RefreshToken! },
            ct);
        s1Refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        var s1New = await s1Refreshed.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);

        (await PostBearerStatusAsync(harness.Client, "/auth/revoke", s1New!.AccessToken!, new JwtRevokeRequest { Scope = "allSessions" }, ct))
            .Should().Be(HttpStatusCode.NoContent);

        (await GetBearerStatusAsync(harness.Client, "/resource/protected", s1New.AccessToken!, ct)).Should().Be(HttpStatusCode.Unauthorized);
        (await GetBearerStatusAsync(harness.Client, "/resource/protected", s2.AccessToken!, ct)).Should().Be(HttpStatusCode.Unauthorized);

        var immediateRelogin = await LoginAsync(harness.Client, "revoke-user", "Secret123!", ct);
        (await GetBearerStatusAsync(harness.Client, "/resource/protected", immediateRelogin.AccessToken!, ct))
            .Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MapInboundClaimsBothFalseAndTrueWorkAcrossProtectedAndRevokeEndpointsAsync(bool mapInbound)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync(o => o.MapInboundClaims = mapInbound);
        await harness.SeedUserAsync("claims-mode-user", "Secret123!");

        var login = await LoginAsync(harness.Client, "claims-mode-user", "Secret123!", ct);
        (await GetBearerStatusAsync(harness.Client, "/auth/mfa/status", login.AccessToken!, ct)).Should().Be(HttpStatusCode.OK);
        (await PostBearerStatusAsync(harness.Client, "/auth/revoke", login.AccessToken!, new JwtRevokeRequest { Scope = "session" }, ct))
            .Should().Be(HttpStatusCode.NoContent);
        (await GetBearerStatusAsync(harness.Client, "/resource/protected", login.AccessToken!, ct)).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(RevocationFailureMode.FailClosed, HttpStatusCode.Unauthorized)]
    [InlineData(RevocationFailureMode.FailOpen, HttpStatusCode.OK)]
    public async Task RevocationStoreExceptionRespectsConfiguredFailureModeAsync(
        RevocationFailureMode mode,
        HttpStatusCode expectedStatus)
    {
        var ct = TestContext.Current.CancellationToken;
        var throwingStore = new FaultyRevocationStore();
        await using var harness = await JwtTestHostHarness.CreateAsync(
            configureOptions: o => o.FailureMode = mode,
            configureServices: s => s.Replace(ServiceDescriptor.Singleton<ITokenRevocationStore>(throwingStore)));
        await harness.SeedUserAsync("outage-user", "Secret123!");

        var login = await LoginAsync(harness.Client, "outage-user", "Secret123!", ct);
        throwingStore.ShouldThrow = true;

        (await GetBearerStatusAsync(harness.Client, "/resource/protected", login.AccessToken!, ct))
            .Should().Be(expectedStatus);
    }

    [Fact]
    public async Task RegisterEndpointIsOptInAndImmuneToMassAssignmentRoleInjectionAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var disabledHarness = await JwtTestHostHarness.CreateAsync();
        (await PostJsonStatusAsync(disabledHarness.Client, "/auth/register", new { email = "new@example.test", password = "Secret123!" }, ct))
            .Should().Be(HttpStatusCode.NotFound);

        await using var enabledHarness = await JwtTestHostHarness.CreateAsync(
            configureEndpoints: e => e.EnableRegistration = true);
        (await PostJsonStatusAsync(
                enabledHarness.Client,
                "/auth/register",
                new
                {
                    userName = "registered-user",
                    email = "registered@example.test",
                    password = "Secret123!",
                    roles = new[] { "Admin" },
                    isAdmin = true,
                },
                ct))
            .Should().Be(HttpStatusCode.Created);

        var createdUser = enabledHarness.UserStore.Users.Single(
            u => string.Equals(u.UserName, "registered-user", StringComparison.Ordinal));
        createdUser.Roles.Should().BeEmpty();
        createdUser.Claims.Should().BeEmpty();
    }

    private static async Task<JwtLoginResponse> LoginAsync(
        HttpClient client,
        string userName,
        string password,
        CancellationToken ct)
    {
        using var resp = await client.PostAsJsonAsync(
            "/auth/login",
            new JwtLoginRequest { UserName = userName, Password = password },
            ct);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await resp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);
        return payload!;
    }

    private static async Task<HttpStatusCode> PostJsonStatusAsync<TBody>(
        HttpClient client,
        string path,
        TBody body,
        CancellationToken ct)
    {
        using var resp = await client.PostAsJsonAsync(path, body, ct);
        return resp.StatusCode;
    }

    private static async Task<HttpStatusCode> GetBearerStatusAsync(
        HttpClient client,
        string path,
        string bearerToken,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        using var resp = await client.SendAsync(req, ct);
        return resp.StatusCode;
    }

    private static async Task<HttpStatusCode> PostBearerStatusAsync<TBody>(
        HttpClient client,
        string path,
        string bearerToken,
        TBody body,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        using var resp = await client.SendAsync(req, ct);
        return resp.StatusCode;
    }

    private sealed class FaultyRevocationStore : ITokenRevocationStore
    {
        public bool ShouldThrow { get; set; }

        public Task RevokeAsync(TokenRevocationEntry entry, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> IsRevokedAsync(TokenRevocationCheckContext context, CancellationToken cancellationToken) =>
            ShouldThrow
                ? Task.FromException<bool>(new InvalidOperationException("Simulated store outage"))
                : Task.FromResult(false);

        public Task<int> PruneExpiredAsync(int batchSize, CancellationToken cancellationToken) =>
            Task.FromResult(0);
    }
}
