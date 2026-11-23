using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Authorization;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test;

public sealed class StandaloneResourceValidationTests
{
    [Fact]
    public async Task StandaloneResourceApiValidatesPublicKeyTokensAndMfaPolicyWithoutIdentityStoresAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privatePem = ecdsa.ExportECPrivateKeyPem();
        var publicPem = ecdsa.ExportSubjectPublicKeyInfoPem();
        var time = new AdjustableTimeProvider();

        var (pwdToken, mfaToken) = MintTokens(privatePem, time);
        await using var app = await StartStandaloneResourceApiAsync(publicPem, time, ct);
        app.Services.GetService<UserManager<TestUser>>().Should().BeNull();

        using var client = app.GetTestClient();
        (await SendStatusAsync(client, HttpMethod.Get, "/orders", pwdToken.AccessToken, ct)).Should().Be(HttpStatusCode.OK);
        (await SendStatusAsync(client, HttpMethod.Delete, "/orders/42", pwdToken.AccessToken, ct)).Should().Be(HttpStatusCode.Forbidden);
        (await SendStatusAsync(client, HttpMethod.Delete, "/orders/42", mfaToken.AccessToken, ct)).Should().Be(HttpStatusCode.NoContent);

        var revocationStore = app.Services.GetRequiredService<ITokenRevocationStore>();
        await revocationStore.RevokeAsync(
            new TokenRevocationEntry(
                "https://idp.example.test",
                RevocationSubjectType.Jti,
                mfaToken.JwtId,
                time.GetUtcNow(),
                mfaToken.ExpiresAtUtc.AddMinutes(1),
                null,
                "revoked-downstream"),
            ct);

        (await SendStatusAsync(client, HttpMethod.Delete, "/orders/42", mfaToken.AccessToken, ct)).Should().Be(HttpStatusCode.Unauthorized);
    }

    private static (IssuedAccessToken PwdToken, IssuedAccessToken MfaToken) MintTokens(
        string privatePem,
        TimeProvider time)
    {
        var issuerOptions = new IdentityJwtOptions
        {
            Issuer = "https://idp.example.test",
            TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        };
        issuerOptions.Audiences.Add("https://orders-api.example.test");
        issuerOptions.SigningKeys.Add(new JwtSigningKeyDescriptor
        {
            Kid = "ec-idp-1",
            Algorithm = JwtKeyAlgorithm.ES256,
            Pem = privatePem,
            IsActiveSigningKey = true,
        });

        var wrapped = MSOptions.Create(issuerOptions);
        using var signer = new DefaultJwtSigningCredentialsProvider(wrapped);
        var generator = new JwtTokenGenerator(wrapped, signer, time);
        var passwordToken = generator.GenerateAccessToken(new JwtTokenDescriptorContext("u-10", "s-10", ["pwd"], [new Claim("scope", "orders.read")]));
        var stepUpToken = generator.GenerateAccessToken(new JwtTokenDescriptorContext("u-10", "s-11", ["pwd", "mfa"], [new Claim("scope", "orders.write")]));
        return (passwordToken, stepUpToken);
    }

    private static async Task<WebApplication> StartStandaloneResourceApiAsync(
        string publicPem,
        TimeProvider time,
        CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(time);
        builder.Services
            .AddIdentityJwtValidation(v =>
            {
                v.Issuer = "https://idp.example.test";
                v.Audiences.Add("https://orders-api.example.test");
                v.SigningKeys.Add(new JwtSigningKeyDescriptor
                {
                    Kid = "ec-idp-1",
                    Algorithm = JwtKeyAlgorithm.ES256,
                    Pem = publicPem,
                    IsActiveSigningKey = false,
                });
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("StepUpMfa", p => p.RequireMfaAssurance());

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/orders", () => Results.Ok(new { Items = 3 })).RequireAuthorization();
        app.MapDelete("/orders/42", () => Results.NoContent()).RequireAuthorization("StepUpMfa");
        await app.StartAsync(ct);
        return app;
    }

    private static async Task<HttpStatusCode> SendStatusAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string bearerToken,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        using var resp = await client.SendAsync(req, ct);
        return resp.StatusCode;
    }
}
