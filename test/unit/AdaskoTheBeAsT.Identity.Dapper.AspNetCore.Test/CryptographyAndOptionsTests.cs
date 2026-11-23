using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test;

public sealed class CryptographyAndOptionsTests
{
    [Fact]
    public void HmacHasherDeterministicallyHashesAndSupportsKeyRotation()
    {
        var oldKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var activeKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var v1Options = new IdentityJwtOptions
        {
            TokenHashKeyId = "hk-v1",
            TokenHashKey = oldKey,
        };
        var v1Hasher = new HmacTokenSecretHasher(MSOptions.Create(v1Options));
        var rawToken = v1Hasher.GenerateOpaqueToken();
        var v1Hashed = v1Hasher.HashToken(rawToken);
        var v1HashedAgain = v1Hasher.HashToken(rawToken);

        v1Hashed.KeyId.Should().Be("hk-v1");
        v1Hashed.HashHex.Should().Be(v1HashedAgain.HashHex);
        v1Hashed.HashHex.Should().HaveLength(64);

        var v2Options = new IdentityJwtOptions
        {
            TokenHashKeyId = "hk-v2",
            TokenHashKey = activeKey,
        };
        v2Options.PreviousTokenHashKeys["hk-v1"] = oldKey;
        var v2Hasher = new HmacTokenSecretHasher(MSOptions.Create(v2Options));

        v2Hasher.VerifyToken(rawToken, v1Hashed.HashHex, v1Hashed.KeyId).Should().BeTrue();
        v2Hasher.VerifyToken(rawToken + "tampered", v1Hashed.HashHex, v1Hashed.KeyId).Should().BeFalse();
        v2Hasher.VerifyToken(rawToken, v1Hashed.HashHex, "unknown-kid").Should().BeFalse();
        v2Hasher.ComputeCandidateTokenHashes(rawToken).Should().HaveCount(2);
    }

    [Fact]
    public void HmacHasherScopesOtpSecretsAndGeneratesSixDigitCodes()
    {
        var options = new IdentityJwtOptions
        {
            TokenHashKeyId = "hk-otp",
            TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        };
        var hasher = new HmacTokenSecretHasher(MSOptions.Create(options));
        var code = hasher.GenerateSixDigitCode();

        code.Should().HaveLength(6);
        code.All(char.IsAsciiDigit).Should().BeTrue();

        var scopeA = $"mfa-otp:{Guid.NewGuid():N}";
        var scopeB = $"mfa-otp:{Guid.NewGuid():N}";
        var scopedA = hasher.HashScopedCode(scopeA, code);
        var scopedB = hasher.HashScopedCode(scopeB, code);

        scopedA.HashHex.Should().NotBe(scopedB.HashHex);
        hasher.VerifyScopedCode(scopeA, code, scopedA.HashHex, scopedA.KeyId).Should().BeTrue();
        hasher.VerifyScopedCode(scopeB, code, scopedA.HashHex, scopedA.KeyId).Should().BeFalse();
    }

    [Theory]
    [InlineData(JwtKeyAlgorithm.ES256)]
    [InlineData(JwtKeyAlgorithm.ES384)]
    [InlineData(JwtKeyAlgorithm.PS256)]
    [InlineData(JwtKeyAlgorithm.RS256)]
    [InlineData(JwtKeyAlgorithm.HS256)]
    public async Task AllFiveSupportedAlgorithmsSignAndValidateAccessTokensAsync(JwtKeyAlgorithm algorithm)
    {
        var descriptor = CreateKeyDescriptor("k-main", algorithm, canSign: true);
        var options = CreateValidIssuerOptions(descriptor);
        var validationOptions = CreateValidationOptionsFromIssuer(options);

        using var signingProvider = new DefaultJwtSigningCredentialsProvider(MSOptions.Create(options));
        using var validationProvider = new DefaultJwtValidationKeysProvider(MSOptions.Create(validationOptions));
        var generator = new JwtTokenGenerator(MSOptions.Create(options), signingProvider, new AdjustableTimeProvider());

        var issued = generator.GenerateAccessToken(
            new JwtTokenDescriptorContext(
                "user-42",
                "sess-99",
                ["pwd", "mfa"],
                [new Claim("tenant", "alpha")]));

        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        var result = await handler.ValidateTokenAsync(
            issued.AccessToken,
            BuildValidationParameters(validationOptions, validationProvider));

        result.IsValid.Should().BeTrue();
        result.ClaimsIdentity.FindFirst("sub")!.Value.Should().Be("user-42");
        result.ClaimsIdentity.FindFirst("sid")!.Value.Should().Be("sess-99");
        result.ClaimsIdentity.FindFirst("tenant")!.Value.Should().Be("alpha");
    }

    [Fact]
    public async Task KidKeyRotationValidatesTokensSignedByRetiredAndActiveKeysAsync()
    {
        using var ecOld = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var ecNew = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var oldPublic = ecOld.ExportSubjectPublicKeyInfoPem();
        var newPublic = ecNew.ExportSubjectPublicKeyInfoPem();
        var time = new AdjustableTimeProvider();

        var oldOptions = CreateValidIssuerOptions(new JwtSigningKeyDescriptor
        {
            Kid = "kid-2025",
            Algorithm = JwtKeyAlgorithm.ES256,
            Pem = ecOld.ExportECPrivateKeyPem(),
            IsActiveSigningKey = true,
        });
        using var oldSigner = new DefaultJwtSigningCredentialsProvider(MSOptions.Create(oldOptions));
        var legacyToken = new JwtTokenGenerator(MSOptions.Create(oldOptions), oldSigner, time)
            .GenerateAccessToken(new JwtTokenDescriptorContext("u-1", "s-1", ["pwd"], []));

        var rotatedOptions = CreateValidIssuerOptions(
            new JwtSigningKeyDescriptor { Kid = "kid-2025", Algorithm = JwtKeyAlgorithm.ES256, Pem = oldPublic, IsActiveSigningKey = false },
            new JwtSigningKeyDescriptor { Kid = "kid-2026", Algorithm = JwtKeyAlgorithm.ES256, Pem = ecNew.ExportECPrivateKeyPem(), IsActiveSigningKey = true });
        using var newSigner = new DefaultJwtSigningCredentialsProvider(MSOptions.Create(rotatedOptions));
        var freshToken = new JwtTokenGenerator(MSOptions.Create(rotatedOptions), newSigner, time)
            .GenerateAccessToken(new JwtTokenDescriptorContext("u-1", "s-2", ["pwd"], []));

        var validatorOptions = CreateValidationOptionsFromIssuer(rotatedOptions);
        validatorOptions.SigningKeys.Clear();
        validatorOptions.SigningKeys.Add(new JwtSigningKeyDescriptor { Kid = "kid-2025", Algorithm = JwtKeyAlgorithm.ES256, Pem = oldPublic, IsActiveSigningKey = false });
        validatorOptions.SigningKeys.Add(new JwtSigningKeyDescriptor { Kid = "kid-2026", Algorithm = JwtKeyAlgorithm.ES256, Pem = newPublic, IsActiveSigningKey = false });
        using var keysProvider = new DefaultJwtValidationKeysProvider(MSOptions.Create(validatorOptions));
        var parameters = BuildValidationParameters(validatorOptions, keysProvider);
        var handler = new JsonWebTokenHandler { MapInboundClaims = false };

        (await handler.ValidateTokenAsync(legacyToken.AccessToken, parameters)).IsValid.Should().BeTrue();
        (await handler.ValidateTokenAsync(freshToken.AccessToken, parameters)).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UnsignedAlgNoneAndAlgorithmConfusionTokensAreRejectedAsync()
    {
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var validationOptions = new IdentityJwtValidationOptions { Issuer = "https://auth.example.test" };
        validationOptions.Audiences.Add("https://api.example.test");
        validationOptions.SigningKeys.Add(new JwtSigningKeyDescriptor
        {
            Kid = "rsa-pub",
            Algorithm = JwtKeyAlgorithm.RS256,
            Pem = publicPem,
            IsActiveSigningKey = false,
        });

        using var keysProvider = new DefaultJwtValidationKeysProvider(MSOptions.Create(validationOptions));
        var parameters = BuildValidationParameters(validationOptions, keysProvider);
        var handler = new JsonWebTokenHandler { MapInboundClaims = false };

        var headerNone = Base64UrlEncoder.Encode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncoder.Encode(
            "{\"iss\":\"https://auth.example.test\",\"aud\":\"https://api.example.test\",\"sub\":\"admin\",\"exp\":4102444800}");
        (await handler.ValidateTokenAsync($"{headerNone}.{payload}.", parameters)).IsValid.Should().BeFalse();

        var confusionKeyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(publicPem));
        var hsKey = new SymmetricSecurityKey(confusionKeyBytes) { KeyId = "rsa-pub" };
        var forged = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "https://auth.example.test",
            Audience = "https://api.example.test",
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(hsKey, SecurityAlgorithms.HmacSha256),
        });
        (await handler.ValidateTokenAsync(forged, parameters)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void OptionsValidatorRejectsWeakKeysMissingSecretsInsecureHttpAndUnkeyedHashOutsideDev()
    {
        var prodValidator = new IdentityJwtOptionsValidator(new HostingEnvironment { EnvironmentName = "Production" });
        prodValidator.Validate(name: null, new IdentityJwtOptions()).Failed.Should().BeTrue();

        var unkeyedInProd = CreateValidIssuerOptions(CreateKeyDescriptor("k1", JwtKeyAlgorithm.ES256, canSign: true));
        unkeyedInProd.TokenHashKey = null;
        unkeyedInProd.AllowUnkeyedSha256InDevelopment = true;
        prodValidator.Validate(name: null, unkeyedInProd).Failed.Should().BeTrue();

        var weakHs = CreateValidIssuerOptions(new JwtSigningKeyDescriptor
        {
            Kid = "weak",
            Algorithm = JwtKeyAlgorithm.HS256,
            SymmetricKey = "too-short",
            IsActiveSigningKey = true,
        });
        prodValidator.Validate(name: null, weakHs).Failed.Should().BeTrue();

        var insecureSetupUri = CreateValidIssuerOptions(CreateKeyDescriptor("k2", JwtKeyAlgorithm.ES256, canSign: true));
        insecureSetupUri.Mfa.PublicSetupBaseUri = new Uri("http://portal.example.test/setup");
        prodValidator.Validate(name: null, insecureSetupUri).Failed.Should().BeTrue();

        var devValidator = new IdentityJwtOptionsValidator(new HostingEnvironment { EnvironmentName = "Development" });
        var devUnkeyedWithOtp = CreateValidIssuerOptions(CreateKeyDescriptor("k-dev-otp", JwtKeyAlgorithm.ES256, canSign: true));
        devUnkeyedWithOtp.TokenHashKey = null;
        devUnkeyedWithOtp.AllowUnkeyedSha256InDevelopment = true;
        devValidator.Validate(name: null, devUnkeyedWithOtp).Failed.Should().BeTrue();

        var devValid = CreateValidIssuerOptions(CreateKeyDescriptor("k-dev", JwtKeyAlgorithm.ES256, canSign: true));
        devValid.TokenHashKey = null;
        devValid.AllowUnkeyedSha256InDevelopment = true;
        devValid.Mfa.AllowEmailMfa = false;
        devValid.Mfa.AllowSmsMfa = false;
        devValidator.Validate(name: null, devValid).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Rfc6238ReferenceHelperMatchesStandardTestVector()
    {
        const string rfcSecretBase32 = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        var vectorTime = DateTimeOffset.FromUnixTimeSeconds(59);
        Rfc6238TestHelper.ComputeTotp(rfcSecretBase32, vectorTime).Should().Be("287082");
    }

    private static JwtSigningKeyDescriptor CreateKeyDescriptor(
        string keyId,
        JwtKeyAlgorithm algorithm,
        bool canSign)
    {
        switch (algorithm)
        {
            case JwtKeyAlgorithm.ES256:
            {
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                return new JwtSigningKeyDescriptor { Kid = keyId, Algorithm = algorithm, Pem = ecdsa.ExportECPrivateKeyPem(), IsActiveSigningKey = canSign };
            }

            case JwtKeyAlgorithm.ES384:
            {
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
                return new JwtSigningKeyDescriptor { Kid = keyId, Algorithm = algorithm, Pem = ecdsa.ExportECPrivateKeyPem(), IsActiveSigningKey = canSign };
            }

            case JwtKeyAlgorithm.PS256:
            case JwtKeyAlgorithm.RS256:
            {
                using var rsa = RSA.Create(2048);
                return new JwtSigningKeyDescriptor { Kid = keyId, Algorithm = algorithm, Pem = rsa.ExportRSAPrivateKeyPem(), IsActiveSigningKey = canSign };
            }

            default:
                return new JwtSigningKeyDescriptor
                {
                    Kid = keyId,
                    Algorithm = JwtKeyAlgorithm.HS256,
                    SymmetricKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                    IsActiveSigningKey = canSign,
                };
        }
    }

    private static IdentityJwtOptions CreateValidIssuerOptions(params JwtSigningKeyDescriptor[] keys)
    {
        var options = new IdentityJwtOptions
        {
            Issuer = "https://auth.example.test",
            TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        };
        options.Audiences.Add("https://api.example.test");
        foreach (var k in keys)
        {
            options.SigningKeys.Add(k);
        }

        return options;
    }

    private static IdentityJwtValidationOptions CreateValidationOptionsFromIssuer(IdentityJwtOptions issuerOptions)
    {
        var validation = new IdentityJwtValidationOptions { Issuer = issuerOptions.Issuer };
        foreach (var aud in issuerOptions.Audiences)
        {
            validation.Audiences.Add(aud);
        }

        foreach (var key in issuerOptions.SigningKeys)
        {
            validation.SigningKeys.Add(key);
        }

        return validation;
    }

    private static TokenValidationParameters BuildValidationParameters(
        IdentityJwtValidationOptions cfg,
        IJwtValidationKeysProvider keysProvider) =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = cfg.Issuer,
            ValidateAudience = true,
            ValidAudiences = cfg.Audiences.ToArray(),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keysProvider.GetValidationKeys(),
            ValidAlgorithms = keysProvider.GetValidAlgorithms(),
            ClockSkew = cfg.ClockSkew,
        };
}
