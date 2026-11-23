using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test;

public sealed class ProblemDetailsContractTests
{
    [Fact]
    public async Task EndpointErrorsReturnStandardProblemDetailsAcrossStatusCodesAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync(
            configureEndpoints: e => e.EnableRegistration = true);
        await harness.SeedUserAsync("problem-user", "Secret123!", "problem-user@example.test");

        using var badLoginResp = await harness.Client.PostAsJsonAsync(
            "/auth/login",
            new JwtLoginRequest { UserName = "problem-user", Password = "WrongPassword!" },
            ct);
        var badLoginProblem = await AssertProblemDetailsAsync(badLoginResp, HttpStatusCode.Unauthorized, "Unauthorized", ct);
        GetExtensionString(badLoginProblem, IdentityJwtProblemDetailsDefaults.ErrorExtensionKey)
            .Should()
            .Be("invalid_grant");

        using var duplicateRegResp = await harness.Client.PostAsJsonAsync(
            "/auth/register",
            new JwtRegisterRequest
            {
                UserName = "problem-user",
                Email = "problem-user@example.test",
                Password = "Secret123!",
            },
            ct);
        var conflictProblem = await AssertProblemDetailsAsync(duplicateRegResp, HttpStatusCode.Conflict, "Conflict", ct);
        GetExtensionString(conflictProblem, IdentityJwtProblemDetailsDefaults.ErrorExtensionKey)
            .Should()
            .NotBeNullOrWhiteSpace();

        using var malformedLoginContent = new StringContent("{invalid-json", Encoding.UTF8, "application/json");
        using var malformedLoginResp = await harness.Client.PostAsync("/auth/login", malformedLoginContent, ct);
        var malformedLoginProblem = await AssertProblemDetailsAsync(malformedLoginResp, HttpStatusCode.BadRequest, "Bad Request", ct);
        GetExtensionString(malformedLoginProblem, IdentityJwtProblemDetailsDefaults.ErrorExtensionKey)
            .Should()
            .Be("invalid_request");

        using var plainTextContent = new StringContent("plain-text", Encoding.UTF8, "text/plain");
        using var unsupportedResp = await harness.Client.PostAsync("/auth/login", plainTextContent, ct);
        var unsupportedProblem = await AssertProblemDetailsAsync(
            unsupportedResp,
            HttpStatusCode.UnsupportedMediaType,
            "Unsupported Media Type",
            ct);
        GetExtensionString(unsupportedProblem, IdentityJwtProblemDetailsDefaults.ErrorExtensionKey)
            .Should()
            .Be("invalid_request");
    }

    [Fact]
    public async Task BearerChallengesForbiddenAndMalformedOptionalJsonReturnStandardProblemDetailsAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync();
        await harness.SeedUserAsync("bearer-problem", "Secret123!");

        using var anonymousResp = await harness.Client.GetAsync("/resource/protected", ct);
        anonymousResp.Headers.WwwAuthenticate.Should().NotBeEmpty();
        var anonymousProblem = await AssertProblemDetailsAsync(anonymousResp, HttpStatusCode.Unauthorized, "Unauthorized", ct);
        GetExtensionString(anonymousProblem, IdentityJwtProblemDetailsDefaults.ErrorExtensionKey)
            .Should()
            .Be("unauthorized");

        using var loginResp = await harness.Client.PostAsJsonAsync(
            "/auth/login",
            new JwtLoginRequest { UserName = "bearer-problem", Password = "Secret123!" },
            ct);
        var login = await loginResp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);

        using var malformedRevokeReq = new HttpRequestMessage(HttpMethod.Post, "/auth/revoke")
        {
            Content = new StringContent("{not-valid", Encoding.UTF8, "application/json"),
        };
        malformedRevokeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        using var malformedRevokeResp = await harness.Client.SendAsync(malformedRevokeReq, ct);
        var malformedProblem = await AssertProblemDetailsAsync(malformedRevokeResp, HttpStatusCode.BadRequest, "Bad Request", ct);
        malformedProblem.Detail.Should().Be("Malformed JSON request body.");
        GetExtensionString(malformedProblem, IdentityJwtProblemDetailsDefaults.ErrorExtensionKey)
            .Should()
            .Be("invalid_request");

        using var forbiddenReq = new HttpRequestMessage(HttpMethod.Get, "/resource/mfa-only");
        forbiddenReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var forbiddenResp = await harness.Client.SendAsync(forbiddenReq, ct);
        var forbiddenProblem = await AssertProblemDetailsAsync(forbiddenResp, HttpStatusCode.Forbidden, "Forbidden", ct);
        GetExtensionString(forbiddenProblem, IdentityJwtProblemDetailsDefaults.ErrorExtensionKey)
            .Should()
            .Be("forbidden");

        using var revokeReq = new HttpRequestMessage(HttpMethod.Post, "/auth/revoke")
        {
            Content = JsonContent.Create(new JwtRevokeRequest { Scope = "token" }),
        };
        revokeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var revokeResp = await harness.Client.SendAsync(revokeReq, ct);
        revokeResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var revokedGetReq = new HttpRequestMessage(HttpMethod.Get, "/resource/protected");
        revokedGetReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var revokedGetResp = await harness.Client.SendAsync(revokedGetReq, ct);
        var revokedProblem = await AssertProblemDetailsAsync(revokedGetResp, HttpStatusCode.Unauthorized, "Unauthorized", ct);
        revokedProblem.Detail.Should().Be("The access token or session has been revoked.");
        GetExtensionString(revokedProblem, IdentityJwtProblemDetailsDefaults.ErrorExtensionKey)
            .Should()
            .Be("invalid_token");
    }

    [Fact]
    public async Task MfaResendCooldownReturnsProblemDetailsWithRetryAfterExtensionAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync();
        var user = await harness.SeedUserAsync("cooldown-problem", "Secret123!", "cooldown-problem@example.test");
        await EnableEmailMfaAsync(harness, user.UserName!, "Secret123!", ct);

        using var mfaChallengeResp = await harness.Client.PostAsJsonAsync(
            "/auth/login",
            new JwtLoginRequest { UserName = user.UserName, Password = "Secret123!" },
            ct);
        var challenge = await mfaChallengeResp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);

        using var cooldownResp = await harness.Client.PostAsJsonAsync(
            "/auth/login/mfa/resend",
            new JwtMfaResendRequest { MfaChallengeToken = challenge!.MfaChallengeToken!, Method = "Email" },
            ct);
        cooldownResp.Headers.RetryAfter.Should().NotBeNull();
        var cooldownProblem = await AssertProblemDetailsAsync(
            cooldownResp,
            HttpStatusCode.TooManyRequests,
            "Too Many Requests",
            ct);
        GetExtensionString(cooldownProblem, IdentityJwtProblemDetailsDefaults.ErrorExtensionKey)
            .Should()
            .Be("resend_cooldown_active");
        GetExtensionInt32(cooldownProblem, IdentityJwtProblemDetailsDefaults.RetryAfterSecondsExtensionKey)
            .Should()
            .BeGreaterThan(0);
    }

    private static async Task EnableEmailMfaAsync(
        JwtTestHostHarness harness,
        string userName,
        string password,
        CancellationToken ct)
    {
        using var pwdResp = await harness.Client.PostAsJsonAsync(
            "/auth/login",
            new JwtLoginRequest { UserName = userName, Password = password },
            ct);
        var pwdLogin = await pwdResp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);

        using var startEmailReq = new HttpRequestMessage(HttpMethod.Post, "/auth/mfa/methods")
        {
            Content = JsonContent.Create(new JwtMfaMethodUpdateRequest { Method = "Email", Enabled = true }),
        };
        startEmailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pwdLogin!.AccessToken);
        using var startEmailResp = await harness.Client.SendAsync(startEmailReq, ct);
        var started = await startEmailResp.Content.ReadFromJsonAsync<JwtMfaMethodUpdateResponse>(ct);

        using var confirmEmailReq = new HttpRequestMessage(HttpMethod.Post, "/auth/mfa/methods")
        {
            Content = JsonContent.Create(new JwtMfaMethodUpdateRequest
            {
                Method = "Email",
                Enabled = true,
                ChallengeToken = started!.ChallengeToken,
                VerificationCode = harness.CodeSender.LastSent!.Code,
            }),
        };
        confirmEmailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pwdLogin.AccessToken);
        using var confirmEmailResp = await harness.Client.SendAsync(confirmEmailReq, ct);
        confirmEmailResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<ProblemDetails> AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedTitle,
        CancellationToken ct)
    {
        response.StatusCode.Should().Be(expectedStatus);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        problem.Should().NotBeNull();
        problem.Status.Should().Be((int)expectedStatus);
        problem.Title.Should().Be(expectedTitle);
        problem.Type.Should().NotBeNullOrWhiteSpace();
        return problem;
    }

    private static string? GetExtensionString(ProblemDetails problem, string key)
    {
        problem.Extensions.TryGetValue(key, out var raw).Should().BeTrue();
        return raw switch
        {
            JsonElement elem => elem.GetString(),
            string str => str,
            _ => raw?.ToString(),
        };
    }

    private static int GetExtensionInt32(ProblemDetails problem, string key)
    {
        problem.Extensions.TryGetValue(key, out var raw).Should().BeTrue();
        return raw is JsonElement elem
            ? elem.GetInt32()
            : Convert.ToInt32(raw, System.Globalization.CultureInfo.InvariantCulture);
    }
}
