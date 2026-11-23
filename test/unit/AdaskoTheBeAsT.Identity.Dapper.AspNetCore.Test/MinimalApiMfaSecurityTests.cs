using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;
using ZXing.Common;
using ZXing.QrCode.Internal;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test;

public sealed class MinimalApiMfaSecurityTests
{
    [Fact]
    public async Task TwoPhaseAuthenticatorSetupProtectsKeyAtRestAndDoesNotEnableBeforeConfirmAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync();
        var user = await harness.SeedUserAsync("totp-user", "Secret123!");
        var pwdLogin = await LoginPasswordAsync(harness.Client, "totp-user", "Secret123!", ct);

        using var setupResp = await SendBearerPostAsync(
            harness.Client,
            "/auth/mfa/authenticator/setup",
            pwdLogin.AccessToken!,
            new JwtMfaAuthenticatorSetupRequest(),
            ct);
        setupResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var setup = await setupResp.Content.ReadFromJsonAsync<JwtMfaAuthenticatorSetupResponse>(ct);

        user.TwoFactorEnabled.Should().BeFalse();
        setup!.QrCodeSvg.Should().BeNull();
        var pendingStored = user.Tokens[("[IdentityJwtMfa]", "PendingAuthenticatorKey")];
        pendingStored.Should().StartWith("dpv1:");
        pendingStored.Should().NotContain(setup.SharedKey.Replace(" ", string.Empty, StringComparison.Ordinal));

        var code = Rfc6238TestHelper.ComputeTotp(setup.SharedKey, harness.TimeProvider.GetUtcNow());
        using var confirmResp = await SendBearerPostAsync(
            harness.Client,
            "/auth/mfa/authenticator/confirm",
            pwdLogin.AccessToken!,
            new JwtMfaAuthenticatorConfirmRequest { Code = code },
            ct);
        confirmResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmed = await confirmResp.Content.ReadFromJsonAsync<JwtMfaAuthenticatorConfirmResponse>(ct);

        confirmed!.TwoFactorEnabled.Should().BeTrue();
        confirmed.RecoveryCodes.Should().HaveCount(10);
        user.TwoFactorEnabled.Should().BeTrue();
        user.Tokens[("[AspNetUserStore]", "AuthenticatorKey")].Should().StartWith("dpv1:");
        user.Tokens.ContainsKey(("[IdentityJwtMfa]", "PendingAuthenticatorKey")).Should().BeFalse();
    }

    [Fact]
    public async Task EnabledAuthenticatorSvgDecodesToTheExactEnrollmentUriAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync(
            configureOptions: o => o.Mfa.AuthenticatorIssuer = "Example: A&B",
            configureBuilder: b => b.AddMfaQrCodeSvg());
        await harness.SeedUserAsync("qr-user", "Secret123!", "qr+test@example.test");
        var login = await LoginPasswordAsync(harness.Client, "qr-user", "Secret123!", ct);

        using var response = await SendBearerPostAsync(
            harness.Client, "/auth/mfa/authenticator/setup", login.AccessToken!, new JwtMfaAuthenticatorSetupRequest(), ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var setup = (await response.Content.ReadFromJsonAsync<JwtMfaAuthenticatorSetupResponse>(ct))!;
        var secret = setup.SharedKey.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        secret.Should().MatchRegex("^[A-Z2-7]{32}$");
        setup.AuthenticatorUri.Should().Be(
            $"otpauth://totp/Example%3A%20A%26B:qr%2Btest%40example.test?secret={secret}&issuer=Example%3A%20A%26B&digits=6&period=30");
        DecodeSvgQrCode(setup.QrCodeSvg!).Should().Be(setup.AuthenticatorUri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CustomQrRendererWinsRegardlessOfRegistrationOrder(bool customFirst)
    {
        var services = new ServiceCollection();
        var builder = services.AddIdentityJwtApi<TestUser>(_ => { });
        if (customFirst)
        {
            builder.AddMfaQrCodeGenerator<TestUser, TestQrCodeGenerator>().AddMfaQrCodeSvg();
        }
        else
        {
            builder.AddMfaQrCodeSvg().AddMfaQrCodeGenerator<TestUser, TestQrCodeGenerator>();
        }

        services.Should().ContainSingle(d => d.ServiceType == typeof(IMfaQrCodeGenerator));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IMfaQrCodeGenerator) && d.ImplementationType == typeof(TestQrCodeGenerator));
    }

    [Theory]
    [InlineData(-2, false)]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public async Task ExistingAuthenticatorAcceptsRfc6238CodeOnlyWithinOneTimeStepAsync(int offset, bool accepted)
    {
        const string existingKey = "gezd-gnbv gy3t-qojq gezd-gnbv gy3t-qojq";
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync();
        var user = await harness.SeedUserAsync("rfc-user", "Secret123!");
        await harness.UserStore.SetAuthenticatorKeyAsync(user, existingKey, ct);
        await harness.UserStore.SetTwoFactorEnabledAsync(user, true, ct);

        // RFC 6238 Appendix B SHA-1 vector at 1_111_111_109 seconds: 07081804, truncated to six digits.
        var codeTime = DateTimeOffset.FromUnixTimeSeconds(1_111_111_109);
        harness.TimeProvider.SetUtcNow(codeTime.AddSeconds(30 * offset));

        var challenge = await LoginPasswordAsync(harness.Client, "rfc-user", "Secret123!", ct);
        challenge.TwoFactorRequired.Should().BeTrue();
        challenge.AccessToken.Should().BeNull();
        var status = await PostJsonStatusAsync(
            harness.Client,
            "/auth/login/mfa",
            new JwtMfaVerifyRequest { MfaChallengeToken = challenge.MfaChallengeToken!, Method = "Authenticator", Code = "081-804" },
            ct);
        status.Should().Be(accepted ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MfaLoginEnforcesChallengeFiveAttemptCapAndTotpSingleUseReplayGuardAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync();
        var user = await harness.SeedUserAsync("mfa-guard", "Secret123!");
        var (sharedKey, _) = await EnrollAuthenticatorAsync(harness, "mfa-guard", "Secret123!", ct);
        harness.TimeProvider.Advance(TimeSpan.FromSeconds(30));

        var challengeLogin = await LoginPasswordAsync(harness.Client, "mfa-guard", "Secret123!", ct);
        challengeLogin.TwoFactorRequired.Should().BeTrue();
        challengeLogin.AccessToken.Should().BeNull();
        challengeLogin.MfaChallengeToken.Should().NotBeNullOrWhiteSpace();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var status = await PostJsonStatusAsync(
                harness.Client,
                "/auth/login/mfa",
                new JwtMfaVerifyRequest { MfaChallengeToken = challengeLogin.MfaChallengeToken, Method = "Authenticator", Code = "000000" },
                ct);
            status.Should().Be(HttpStatusCode.Unauthorized);
        }

        user.AccessFailedCount.Should().BeGreaterThan(0);

        var validCode = Rfc6238TestHelper.ComputeTotp(sharedKey, harness.TimeProvider.GetUtcNow());
        (await PostJsonStatusAsync(
                harness.Client,
                "/auth/login/mfa",
                new JwtMfaVerifyRequest { MfaChallengeToken = challengeLogin.MfaChallengeToken, Method = "Authenticator", Code = validCode },
                ct))
            .Should().Be(HttpStatusCode.Unauthorized);

        var challenge2 = await LoginPasswordAsync(harness.Client, "mfa-guard", "Secret123!", ct);
        (await PostJsonStatusAsync(
                harness.Client,
                "/auth/login/mfa",
                new JwtMfaVerifyRequest { MfaChallengeToken = challenge2.MfaChallengeToken!, Method = "Authenticator", Code = validCode },
                ct))
            .Should().Be(HttpStatusCode.OK);

        var challenge3 = await LoginPasswordAsync(harness.Client, "mfa-guard", "Secret123!", ct);
        (await PostJsonStatusAsync(
                harness.Client,
                "/auth/login/mfa",
                new JwtMfaVerifyRequest { MfaChallengeToken = challenge3.MfaChallengeToken!, Method = "Authenticator", Code = validCode },
                ct))
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmailOtpResendEnforcesCooldownRotatesCodeAndPreservesMfaAmrAcrossRefreshAsync(bool distributed)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync(
            configureBuilder: builder =>
            {
                if (distributed)
                {
                    builder.Services.AddDistributedMemoryCache();
                    builder.AddDistributedCacheStores<TestUser, TestAtomicMfaChallengeCache>();
                }
            });
        await harness.SeedUserAsync("email-mfa", "Secret123!", "email-mfa@example.test");
        var (_, recoveryCodes) = await EnrollAuthenticatorAsync(harness, "email-mfa", "Secret123!", ct);
        await EnableEmailMfaMethodAsync(harness, "email-mfa", "Secret123!", recoveryCodes[0], ct);

        var loginChallenge = await LoginPasswordAsync(harness.Client, "email-mfa", "Secret123!", ct);
        (await PostJsonStatusAsync(
                harness.Client,
                "/auth/login/mfa/resend",
                new JwtMfaResendRequest { MfaChallengeToken = loginChallenge.MfaChallengeToken!, Method = "Email" },
                ct))
            .Should().Be(HttpStatusCode.OK);

        using var cooldownHit = await harness.Client.PostAsJsonAsync(
            "/auth/login/mfa/resend",
            new JwtMfaResendRequest { MfaChallengeToken = loginChallenge.MfaChallengeToken!, Method = "Email" },
            ct);
        cooldownHit.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        cooldownHit.Headers.RetryAfter.Should().NotBeNull();

        harness.TimeProvider.Advance(TimeSpan.FromSeconds(35));
        var resends = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => PostJsonStatusAsync(
                harness.Client,
                "/auth/login/mfa/resend",
                new JwtMfaResendRequest { MfaChallengeToken = loginChallenge.MfaChallengeToken!, Method = "Email" },
                ct)));
        resends.Count(status => status == HttpStatusCode.OK).Should().Be(1);
        resends.Count(status => status == HttpStatusCode.TooManyRequests).Should().Be(15);
        var activeOtp = harness.CodeSender.LastSent!.Code;

        using var verifyResp = await harness.Client.PostAsJsonAsync(
            "/auth/login/mfa",
            new JwtMfaVerifyRequest { MfaChallengeToken = loginChallenge.MfaChallengeToken!, Method = "Email", Code = activeOtp },
            ct);
        verifyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var issued = await verifyResp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);

        using var refreshedResp = await harness.Client.PostAsJsonAsync(
            "/auth/refresh",
            new JwtRefreshRequest { RefreshToken = issued!.RefreshToken! },
            ct);
        var refreshed = await refreshedResp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(refreshed!.AccessToken);
        jwt.Claims.Where(c => string.Equals(c.Type, "amr", StringComparison.Ordinal)).Select(c => c.Value).Should().Contain("mfa");

        using var mfaReq = new HttpRequestMessage(HttpMethod.Get, "/resource/mfa-only");
        mfaReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        using var mfaResp = await harness.Client.SendAsync(mfaReq, ct);
        mfaResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StepUpAssuranceRequiredOnDisableAndRegenerateAndRecoveryCodesAreSingleUseAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync();
        await harness.SeedUserAsync("stepup-user", "Secret123!");
        var pwdBeforeMfa = await LoginPasswordAsync(harness.Client, "stepup-user", "Secret123!", ct);
        var (_, recoveryCodes) = await EnrollAuthenticatorAsync(harness, "stepup-user", "Secret123!", ct);

        (await PostBearerStatusAsync(harness.Client, "/auth/mfa/recovery-codes/regenerate", pwdBeforeMfa.AccessToken!, new { }, ct))
            .Should().Be(HttpStatusCode.Forbidden);
        (await PostBearerStatusAsync(harness.Client, "/auth/mfa/disable", pwdBeforeMfa.AccessToken!, new JwtMfaDisableRequest(), ct))
            .Should().Be(HttpStatusCode.Forbidden);
        (await PostBearerStatusAsync(harness.Client, "/auth/mfa/disable", pwdBeforeMfa.AccessToken!, new JwtMfaDisableRequest { Password = "WrongPassword!" }, ct))
            .Should().Be(HttpStatusCode.Forbidden);

        var recLogin = await LoginWithRecoveryCodeAsync(harness.Client, "stepup-user", "Secret123!", recoveryCodes[0], ct);

        var replayChallenge = await LoginPasswordAsync(harness.Client, "stepup-user", "Secret123!", ct);
        (await PostJsonStatusAsync(
                harness.Client,
                "/auth/login/mfa",
                new JwtMfaVerifyRequest { MfaChallengeToken = replayChallenge.MfaChallengeToken!, Method = "RecoveryCode", Code = recoveryCodes[0] },
                ct))
            .Should().Be(HttpStatusCode.Unauthorized);

        (await PostBearerStatusAsync(harness.Client, "/auth/mfa/recovery-codes/regenerate", recLogin.AccessToken!, new { }, ct))
            .Should().Be(HttpStatusCode.OK);
        (await PostBearerStatusAsync(harness.Client, "/auth/mfa/disable", recLogin.AccessToken!, new JwtMfaDisableRequest(), ct))
            .Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MandatoryEnrollmentAndSetupLinkIgnoreSpoofedHostHeaderAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var defaultHarness = await JwtTestHostHarness.CreateAsync();
        await defaultHarness.SeedUserAsync("optin-user", "Secret123!");
        var optInLogin = await LoginPasswordAsync(defaultHarness.Client, "optin-user", "Secret123!", ct);
        (await PostBearerStatusAsync(defaultHarness.Client, "/auth/mfa/setup-link", optInLogin.AccessToken!, new JwtMfaSetupLinkRequest { Channel = "Email" }, ct))
            .Should().Be(HttpStatusCode.NotFound);

        await using var harness = await JwtTestHostHarness.CreateAsync(
            configureEndpoints: e => e.EnableMfaSetupLinkEndpoints = true);
        await harness.SeedUserAsync("link-user", "Secret123!", "link-user@example.test");
        var pwdLogin = await LoginPasswordAsync(harness.Client, "link-user", "Secret123!", ct);

        using var spoofedReq = new HttpRequestMessage(HttpMethod.Post, "/auth/mfa/setup-link")
        {
            Content = JsonContent.Create(new JwtMfaSetupLinkRequest { Channel = "Email" }),
        };
        spoofedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pwdLogin.AccessToken);
        spoofedReq.Headers.Host = "evil-attacker.example.org";

        using var linkResp = await harness.Client.SendAsync(spoofedReq, ct);
        linkResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var setupUri = harness.CodeSender.LastSetupUri!.ToString();
        setupUri.Should().StartWith("https://portal.example.test/security/mfa-setup");
        setupUri.Should().NotContain("evil-attacker");

        var match = Regex.Match(
            setupUri,
            @"token=(?<tok>[^&\s]+)",
            RegexOptions.ExplicitCapture,
            TimeSpan.FromSeconds(1));
        match.Success.Should().BeTrue();
        var rawSetupToken = Uri.UnescapeDataString(match.Groups["tok"].Value);

        (await PostJsonStatusAsync(
                harness.Client,
                "/auth/mfa/setup-link/exchange",
                new JwtMfaSetupLinkExchangeRequest { SetupToken = rawSetupToken },
                ct))
            .Should().Be(HttpStatusCode.OK);

        await using var mandatoryHarness = await JwtTestHostHarness.CreateAsync(
            o => o.Mfa.EnforcementMode = MfaEnforcementMode.RequiredForAllUsers);
        await mandatoryHarness.SeedUserAsync("unenrolled-user", "Secret123!");
        var mandatoryLogin = await LoginPasswordAsync(mandatoryHarness.Client, "unenrolled-user", "Secret123!", ct);
        mandatoryLogin.MfaEnrollmentRequired.Should().BeTrue();
        mandatoryLogin.AccessToken.Should().BeNull();
        mandatoryLogin.MfaChallengeToken.Should().NotBeNullOrWhiteSpace();
    }

    private static async Task EnableEmailMfaMethodAsync(
        JwtTestHostHarness harness,
        string userName,
        string password,
        string recoveryCode,
        CancellationToken ct)
    {
        var mfaSession = await LoginWithRecoveryCodeAsync(harness.Client, userName, password, recoveryCode, ct);
        using var initResp = await SendBearerPostAsync(
            harness.Client,
            "/auth/mfa/methods",
            mfaSession.AccessToken!,
            new JwtMfaMethodUpdateRequest { Method = "Email", Enabled = true },
            ct);
        initResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var initBody = await initResp.Content.ReadFromJsonAsync<JwtMfaMethodUpdateResponse>(ct);
        var otp = harness.CodeSender.LastSent!.Code;

        using var confirmResp = await SendBearerPostAsync(
            harness.Client,
            "/auth/mfa/methods",
            mfaSession.AccessToken!,
            new JwtMfaMethodUpdateRequest
            {
                Method = "Email",
                Enabled = true,
                ChallengeToken = initBody!.ChallengeToken,
                VerificationCode = otp,
            },
            ct);
        confirmResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static string DecodeSvgQrCode(string svgMarkup)
    {
        const int quietZone = 4;
        var root = XElement.Parse(svgMarkup);
        var ns = XNamespace.Get("http://www.w3.org/2000/svg");
        root.Name.Should().Be(ns + "svg");
        var viewBox = root.Attribute("viewBox")!.Value.Split(' ');
        var side = int.Parse(viewBox[2], CultureInfo.InvariantCulture) - (2 * quietZone);
        var bits = new BitMatrix(side);
        var path = root.Elements(ns + "path").Single().Attribute("d")!.Value;

        // QRCoder represents each run of dark modules as a one-module-high SVG rectangle.
        foreach (Match run in Regex.Matches(
            path, @"M(?<x>\d+) (?<y>\d+)h(?<width>\d+)v1h-\k<width>z", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1)))
        {
            var x = int.Parse(run.Groups["x"].Value, CultureInfo.InvariantCulture) - quietZone;
            var y = int.Parse(run.Groups["y"].Value, CultureInfo.InvariantCulture) - quietZone;
            var width = int.Parse(run.Groups["width"].Value, CultureInfo.InvariantCulture);
            bits.setRegion(x, y, width, 1);
        }

        return new Decoder().decode(bits, null).Text;
    }

    private static async Task<(string SharedKey, IReadOnlyList<string> RecoveryCodes)> EnrollAuthenticatorAsync(
        JwtTestHostHarness harness,
        string userName,
        string password,
        CancellationToken ct)
    {
        var login = await LoginPasswordAsync(harness.Client, userName, password, ct);
        using var setupResp = await SendBearerPostAsync(
            harness.Client,
            "/auth/mfa/authenticator/setup",
            login.AccessToken!,
            new JwtMfaAuthenticatorSetupRequest(),
            ct);
        var setup = await setupResp.Content.ReadFromJsonAsync<JwtMfaAuthenticatorSetupResponse>(ct);
        var code = Rfc6238TestHelper.ComputeTotp(setup!.SharedKey, harness.TimeProvider.GetUtcNow());
        using var confirmResp = await SendBearerPostAsync(
            harness.Client,
            "/auth/mfa/authenticator/confirm",
            login.AccessToken!,
            new JwtMfaAuthenticatorConfirmRequest { Code = code },
            ct);
        var confirmed = await confirmResp.Content.ReadFromJsonAsync<JwtMfaAuthenticatorConfirmResponse>(ct);
        return (setup.SharedKey, confirmed!.RecoveryCodes);
    }

    private static async Task<JwtLoginResponse> LoginWithRecoveryCodeAsync(
        HttpClient client,
        string userName,
        string password,
        string recoveryCode,
        CancellationToken ct)
    {
        var challenge = await LoginPasswordAsync(client, userName, password, ct);
        using var verifyResp = await client.PostAsJsonAsync(
            "/auth/login/mfa",
            new JwtMfaVerifyRequest
            {
                MfaChallengeToken = challenge.MfaChallengeToken!,
                Method = "RecoveryCode",
                Code = recoveryCode,
            },
            ct);
        verifyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await verifyResp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);
        return result!;
    }

    private static async Task<JwtLoginResponse> LoginPasswordAsync(
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
        var result = await resp.Content.ReadFromJsonAsync<JwtLoginResponse>(ct);
        return result!;
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

    private static async Task<HttpStatusCode> PostBearerStatusAsync<TBody>(
        HttpClient client,
        string path,
        string bearerToken,
        TBody body,
        CancellationToken ct)
    {
        using var resp = await SendBearerPostAsync(client, path, bearerToken, body, ct);
        return resp.StatusCode;
    }

    private static async Task<HttpResponseMessage> SendBearerPostAsync<TBody>(
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
        return await client.SendAsync(req, ct);
    }

    private sealed class TestQrCodeGenerator : IMfaQrCodeGenerator
    {
        public string GenerateSvg(string otpAuthUri) => string.Empty;
    }
}
