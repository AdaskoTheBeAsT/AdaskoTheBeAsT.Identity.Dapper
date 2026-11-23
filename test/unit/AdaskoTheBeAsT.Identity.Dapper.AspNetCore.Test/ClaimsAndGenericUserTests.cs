using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Services;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using Xunit;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test;

public sealed class ClaimsAndGenericUserTests
{
    [Fact]
    public async Task DefaultClaimsProviderSupportsGuidIntLongAndStringUserKeysWithFastPathStoreAsync()
    {
        var guidUser = new TestIdentityUser<Guid>(Guid.NewGuid(), "guid-user", "guid@example.test");
        var intUser = new TestIdentityUser<int>(42, "int-user", "int@example.test");
        var longUser = new TestIdentityUser<long>(9_876_543_210L, "long-user", "long@example.test");
        var stringUser = new TestIdentityUser<string>("usr_custom_abc", "str-user", "str@example.test");

        await AssertFastPathClaimsForUserAsync(guidUser, guidUser.Id.ToString("D"));
        await AssertFastPathClaimsForUserAsync(intUser, intUser.Id.ToString(CultureInfo.InvariantCulture));
        await AssertFastPathClaimsForUserAsync(longUser, longUser.Id.ToString(CultureInfo.InvariantCulture));
        await AssertFastPathClaimsForUserAsync(stringUser, stringUser.Id);
    }

    [Fact]
    public async Task DefaultClaimsProviderFallsBackToUserManagerWhenCombinedStoreIsAbsentAsync()
    {
        var user = new TestIdentityUser<int>(7, "fallback-user", "fallback@example.test");
        var storeMock = new Mock<IUserEmailStore<TestIdentityUser<int>>>(MockBehavior.Strict);
        storeMock.As<IUserRoleStore<TestIdentityUser<int>>>()
            .Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Operator" });
        storeMock.As<IUserClaimStore<TestIdentityUser<int>>>()
            .Setup(s => s.GetClaimsAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim> { new("department", "ops") });
        storeMock.Setup(s => s.GetUserNameAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(user.UserName);
        storeMock.Setup(s => s.GetEmailAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(user.Email);
        storeMock.Setup(s => s.Dispose());

        using var manager = CreateUserManager(storeMock.Object);
        using var sp = new ServiceCollection().BuildServiceProvider();
        var provider = new DefaultJwtClaimsProvider<TestIdentityUser<int>>(manager, sp);

        var claims = await provider.GetClaimsAsync(user, TestContext.Current.CancellationToken);

        claims.Should().Contain(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.Ordinal) && string.Equals(c.Value, "Operator", StringComparison.Ordinal));
        claims.Should().Contain(c => string.Equals(c.Type, "department", StringComparison.Ordinal) && string.Equals(c.Value, "ops", StringComparison.Ordinal));
        claims.Should().Contain(c => string.Equals(c.Type, IdentityJwtClaimTypes.Email, StringComparison.Ordinal) && string.Equals(c.Value, "fallback@example.test", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DelegateClaimsProviderEnrichesClaimsThroughBuilderAsync()
    {
        var user = new TestUser { Id = Guid.NewGuid(), UserName = "custom-claims-user" };
        using var store = new InMemoryUserStore();
        await store.CreateAsync(user, TestContext.Current.CancellationToken);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<IUserStore<TestUser>>(store);
        services.AddIdentityCore<TestUser>().AddSignInManager();
        services.AddIdentityJwtApi<TestUser>(o =>
            {
                o.Issuer = "https://auth.example.test";
                o.Audiences.Add("https://api.example.test");
                o.TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                o.SigningKeys.Add(new JwtSigningKeyDescriptor
                {
                    Kid = "hs1",
                    Algorithm = JwtKeyAlgorithm.HS256,
                    SymmetricKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                    IsActiveSigningKey = true,
                });
            })
            .AddClaimsProvider((u, _) =>
                Task.FromResult<IReadOnlyList<Claim>>([new Claim("org_unit", $"unit:{u.UserName}")]));

        await using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IJwtClaimsProvider<TestUser>>();
        var claims = await provider.GetClaimsAsync(user, TestContext.Current.CancellationToken);

        claims.Should().ContainSingle(c =>
            string.Equals(c.Type, "org_unit", StringComparison.Ordinal) &&
            string.Equals(c.Value, "unit:custom-claims-user", StringComparison.Ordinal));
    }

    [Fact]
    public void JwtTokenGeneratorPreventsCustomClaimsFromOverwritingReservedProtocolClaims()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var options = new IdentityJwtOptions
        {
            Issuer = "https://real-issuer.test",
            TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        };
        options.Audiences.Add("https://real-audience.test");
        options.SigningKeys.Add(new JwtSigningKeyDescriptor
        {
            Kid = "ec-1",
            Algorithm = JwtKeyAlgorithm.ES256,
            Pem = ecdsa.ExportECPrivateKeyPem(),
            IsActiveSigningKey = true,
        });

        var wrapped = MSOptions.Create(options);
        using var signingProvider = new DefaultJwtSigningCredentialsProvider(wrapped);
        var generator = new JwtTokenGenerator(wrapped, signingProvider, new AdjustableTimeProvider());

        var maliciousCustomClaims = new[]
        {
            new Claim(IdentityJwtClaimTypes.Subject, "spoofed-admin-id"),
            new Claim(IdentityJwtClaimTypes.Issuer, "https://evil-issuer.test"),
            new Claim(IdentityJwtClaimTypes.Audience, "https://evil-aud.test"),
            new Claim(IdentityJwtClaimTypes.JwtId, "spoofed-jti"),
            new Claim(IdentityJwtClaimTypes.SessionId, "spoofed-sid"),
            new Claim(IdentityJwtClaimTypes.AuthenticationMethods, "mfa"),
            new Claim("iat", "1"),
            new Claim("nbf", "1"),
            new Claim("exp", "9999999999"),
            new Claim("workspace", "legit-workspace"),
        };

        var issued = generator.GenerateAccessToken(
            new JwtTokenDescriptorContext("true-user-id", "true-session-id", ["pwd"], maliciousCustomClaims));

        var parsed = new JsonWebTokenHandler().ReadJsonWebToken(issued.AccessToken);
        parsed.Subject.Should().Be("true-user-id");
        parsed.Issuer.Should().Be("https://real-issuer.test");
        parsed.Audiences.Should().ContainSingle().Which.Should().Be("https://real-audience.test");
        parsed.Id.Should().Be(issued.JwtId);
        parsed.Id.Should().NotBe("spoofed-jti");
        parsed.GetPayloadValue<string>("sid").Should().Be("true-session-id");
        parsed.Claims.Where(c => string.Equals(c.Type, "amr", StringComparison.Ordinal)).Select(c => c.Value).Should().Equal("pwd");
        parsed.GetPayloadValue<string>("workspace").Should().Be("legit-workspace");
    }

    [Fact]
    public async Task RegisterAsyncInitializesNonEmptyGuidWhenInsertOwnIdAttributeIsAppliedAsync()
    {
        InsertOwnIdGuidTestUser? captured = null;
        var storeMock = CreateInsertOwnIdMockStore(u => captured = u);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<IUserStore<InsertOwnIdGuidTestUser>>(storeMock.Object);
        services.AddIdentityCore<InsertOwnIdGuidTestUser>().AddSignInManager();
        services.AddIdentityJwtApi<InsertOwnIdGuidTestUser>(o =>
        {
            o.Issuer = "https://issuer.example.test";
            o.Audiences.Add("https://api.example.test");
            o.TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            o.SigningKeys.Add(new JwtSigningKeyDescriptor
            {
                Kid = "ec-1",
                Algorithm = JwtKeyAlgorithm.ES256,
                Pem = ecdsa.ExportECPrivateKeyPem(),
                IsActiveSigningKey = true,
            });
        });

        await using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IIdentityJwtAuthenticationService<InsertOwnIdGuidTestUser>>();
        var result = await authService.RegisterAsync(
            new JwtRegisterRequest
            {
                UserName = "insert-own-id-user",
                Email = "insertownid@example.test",
                Password = "Strong!Password123",
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        captured.Should().NotBeNull();
        captured.Id.Should().NotBe(Guid.Empty);
        result.Value.UserId.Should().Be(captured.Id.ToString("D"));
    }

    private static Mock<IUserPasswordStore<InsertOwnIdGuidTestUser>> CreateInsertOwnIdMockStore(
        Action<InsertOwnIdGuidTestUser> onCreated)
    {
        var storeMock = new Mock<IUserPasswordStore<InsertOwnIdGuidTestUser>>(MockBehavior.Loose);
        var emailStoreMock = storeMock.As<IUserEmailStore<InsertOwnIdGuidTestUser>>();
        storeMock
            .Setup(s => s.SetUserNameAsync(It.IsAny<InsertOwnIdGuidTestUser>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<InsertOwnIdGuidTestUser, string?, CancellationToken>((u, name, _) => u.UserName = name)
            .Returns(Task.CompletedTask);
        storeMock
            .Setup(s => s.GetUserNameAsync(It.IsAny<InsertOwnIdGuidTestUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InsertOwnIdGuidTestUser u, CancellationToken _) => u.UserName);
        emailStoreMock
            .Setup(s => s.SetEmailAsync(It.IsAny<InsertOwnIdGuidTestUser>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<InsertOwnIdGuidTestUser, string?, CancellationToken>((u, email, _) => u.Email = email)
            .Returns(Task.CompletedTask);
        emailStoreMock
            .Setup(s => s.GetEmailAsync(It.IsAny<InsertOwnIdGuidTestUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InsertOwnIdGuidTestUser u, CancellationToken _) => u.Email);
        storeMock
            .Setup(s => s.CreateAsync(It.IsAny<InsertOwnIdGuidTestUser>(), It.IsAny<CancellationToken>()))
            .Callback<InsertOwnIdGuidTestUser, CancellationToken>((u, _) => onCreated?.Invoke(u))
            .ReturnsAsync(IdentityResult.Success);
        storeMock
            .Setup(s => s.GetUserIdAsync(It.IsAny<InsertOwnIdGuidTestUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InsertOwnIdGuidTestUser u, CancellationToken _) => u.Id.ToString("D"));
        return storeMock;
    }

    private static async Task AssertFastPathClaimsForUserAsync<TKey>(
        TestIdentityUser<TKey> user,
        string expectedId)
        where TKey : IEquatable<TKey>
    {
        var storeMock = new Mock<IUserEmailStore<TestIdentityUser<TKey>>>(MockBehavior.Strict);
        storeMock.As<IUserRoleStore<TestIdentityUser<TKey>>>()
            .Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Admin" });
        storeMock.As<IUserClaimStore<TestIdentityUser<TKey>>>();
        storeMock.Setup(s => s.GetUserIdAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(expectedId);
        storeMock.Setup(s => s.GetUserNameAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(user.UserName);
        storeMock.Setup(s => s.GetEmailAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(user.Email);
        storeMock.Setup(s => s.Dispose());

        var combinedStore = new Mock<IUserRoleClaimStore<TestIdentityUser<TKey>>>(MockBehavior.Strict);
        combinedStore
            .Setup(s => s.GetUserAndRoleClaimsAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim> { new("perm", $"read:{expectedId}") });
        combinedStore.Setup(s => s.Dispose());

        var services = new ServiceCollection();
        services.AddSingleton(combinedStore.Object);
        await using var sp = services.BuildServiceProvider();
        using var manager = CreateUserManager(storeMock.Object);
        var provider = new DefaultJwtClaimsProvider<TestIdentityUser<TKey>>(manager, sp);

        var userId = await manager.GetUserIdAsync(user);
        userId.Should().Be(expectedId);

        var claims = await provider.GetClaimsAsync(user, TestContext.Current.CancellationToken);
        claims.Should().Contain(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.Ordinal) && string.Equals(c.Value, "Admin", StringComparison.Ordinal));
        claims.Should().Contain(c => string.Equals(c.Type, "perm", StringComparison.Ordinal) && string.Equals(c.Value, $"read:{expectedId}", StringComparison.Ordinal));
        combinedStore.Verify(s => s.GetUserAndRoleClaimsAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        storeMock.As<IUserClaimStore<TestIdentityUser<TKey>>>()
            .Verify(s => s.GetClaimsAsync(It.IsAny<TestIdentityUser<TKey>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static UserManager<TUser> CreateUserManager<TUser>(IUserStore<TUser> store)
        where TUser : class =>
        new(
            store,
            MSOptions.Create(new IdentityOptions()),
            new PasswordHasher<TUser>(),
            Array.Empty<IUserValidator<TUser>>(),
            Array.Empty<IPasswordValidator<TUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<UserManager<TUser>>.Instance);
}
