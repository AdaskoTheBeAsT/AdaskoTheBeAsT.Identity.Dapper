using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Services;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Validators;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Test;

public sealed class AuthenticationSafetyTest
{
    [Fact]
    public async Task OmittedRolesSurviveActualMappingAndUpdateAsync()
    {
        var mapper = new WebApiMapper();
        var request = mapper.MapToUpdateUserRequest(new UpdateUserModel { UserName = "renamed" });
        request.Roles.Should().BeNull();
        mapper.MapToUpdateUserRequest(new UpdateUserModel { Roles = Array.Empty<string>() }).Roles!.Should().BeEmpty();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "old" };
        request.UserId = user.Id;
        var manager = CreateManager();
        manager.Setup(m => m.FindByIdAsync(user.Id.ToString("D"))).ReturnsAsync(user);
        manager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var result = await new UpdateUserRequestHandler(manager.Object)
            .Handle(request, cancellationToken: TestContext.Current.CancellationToken);
        result.Succeeded.Should().BeTrue();
        manager.Verify(m => m.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        manager.Verify(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task RefreshFollowsImmutableIdAfterRenameAndRotatesAsync()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "recycled", SecurityStamp = "stamp" };
        var manager = CreateConfiguredAuthManager(user, isLockedOut: false, twoFactorEnabled: false);
        var signIn = CreateSignIn(manager.Object);
        signIn.Setup(s => s.CheckPasswordSignInAsync(user, "password", lockoutOnFailure: true)).ReturnsAsync(SignInResult.Success);
        signIn.Setup(s => s.CanSignInAsync(user)).ReturnsAsync(value: true);
        var claims = new Mock<IUserRoleClaimStore<ApplicationUser>>(MockBehavior.Strict);
        claims.Setup(s => s.GetUserAndRoleClaimsAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Claim>());

        await using var provider = CreateAuthServiceProvider(manager.Object, signIn.Object, claims.Object);
        var authService = provider.GetRequiredService<IIdentityJwtAuthenticationService<ApplicationUser>>();
        var passwordHandler = new AuthPasswordRequestHandler(authService);
        var refreshHandler = new AuthRefreshTokenRequestHandler(authService);

        var original = await passwordHandler.Handle(
            new AuthPasswordRequest { Username = "recycled", Password = "password" },
            TestContext.Current.CancellationToken);

        user.UserName = "renamed";
        manager.Invocations.Clear();

        var refreshed = await refreshHandler.Handle(
            new AuthRefreshTokenRequest { RefreshToken = original.RefreshToken },
            TestContext.Current.CancellationToken);
        new JwtSecurityTokenHandler().ReadJwtToken(refreshed.AccessToken).Subject.Should().Be(user.Id.ToString("D"));
        refreshed.RefreshToken.Should().NotBe(original.RefreshToken);
        refreshed.UserName.Should().Be(user.UserName);

        await FluentActions.Awaiting(() =>
            refreshHandler.Handle(new AuthRefreshTokenRequest { RefreshToken = original.RefreshToken }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowExactlyAsync<InvalidRefreshTokenException>();
        manager.Verify(m => m.FindByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("deleted")]
    [InlineData("stamp")]
    [InlineData("locked")]
    [InlineData("not-allowed")]
    public async Task RefreshRejectsInvalidAccountStateAsync(string state)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "initial", SecurityStamp = "original" };
        var manager = CreateConfiguredAuthManager(user, isLockedOut: false, twoFactorEnabled: false);
        var signIn = CreateSignIn(manager.Object);
        signIn.Setup(s => s.CheckPasswordSignInAsync(user, "password", lockoutOnFailure: true)).ReturnsAsync(SignInResult.Success);
        signIn.Setup(s => s.CanSignInAsync(user)).ReturnsAsync(value: true);
        var claims = new Mock<IUserRoleClaimStore<ApplicationUser>>(MockBehavior.Strict);
        claims.Setup(s => s.GetUserAndRoleClaimsAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Claim>());

        await using var provider = CreateAuthServiceProvider(manager.Object, signIn.Object, claims.Object);
        var authService = provider.GetRequiredService<IIdentityJwtAuthenticationService<ApplicationUser>>();
        var issued = await new AuthPasswordRequestHandler(authService).Handle(
            new AuthPasswordRequest { Username = "initial", Password = "password" },
            TestContext.Current.CancellationToken);

        if (string.Equals(state, "stamp", StringComparison.Ordinal))
        {
            user.SecurityStamp = "changed";
        }

        manager.Setup(m => m.FindByIdAsync(user.Id.ToString("D")))
            .ReturnsAsync(string.Equals(state, "deleted", StringComparison.Ordinal) ? null : user);
        manager.Setup(m => m.IsLockedOutAsync(user))
            .ReturnsAsync(string.Equals(state, "locked", StringComparison.Ordinal));
        signIn.Setup(s => s.CanSignInAsync(user))
            .ReturnsAsync(!string.Equals(state, "not-allowed", StringComparison.Ordinal));

        var handler = new AuthRefreshTokenRequestHandler(authService);
        await FluentActions.Awaiting(() =>
            handler.Handle(new AuthRefreshTokenRequest { RefreshToken = issued.RefreshToken }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowExactlyAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task ConcurrentRefreshConsumptionHasOnlyOneWinnerAsync()
    {
        using var tokens = CreateTokens();
        var issued = tokens.GenerateToken(new ApplicationUser { Id = Guid.NewGuid() }, new List<string>(), new List<Claim>());
        var outcomes = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => Task.Run(
                () =>
                {
                    try
                    {
                        tokens.ConsumeRefreshToken(issued.RefreshToken!);
                        return true;
                    }
                    catch (InvalidRefreshTokenException)
                    {
                        return false;
                    }
                },
                TestContext.Current.CancellationToken)));
        outcomes.Should().ContainSingle(result => result);
    }

    [Fact]
    public async Task PasswordAuthenticationUsesFrameworkLockoutChecksAsync()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "locked", LockoutEnabled = true, LockoutEnd = DateTimeOffset.UtcNow.AddDays(1) };
        var manager = CreateConfiguredAuthManager(user, isLockedOut: true, twoFactorEnabled: false);
        var signIn = new SignInManager<ApplicationUser>(
            manager.Object,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(MockBehavior.Strict),
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<ApplicationUser>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(MockBehavior.Strict),
            new DefaultUserConfirmation<ApplicationUser>());

        await using var provider = CreateAuthServiceProvider(
            manager.Object,
            signIn,
            Mock.Of<IUserRoleClaimStore<ApplicationUser>>(MockBehavior.Strict));
        var handler = new AuthPasswordRequestHandler(
            provider.GetRequiredService<IIdentityJwtAuthenticationService<ApplicationUser>>());

        await FluentActions.Awaiting(() =>
            handler.Handle(new AuthPasswordRequest { Username = "locked", Password = "not-evaluated" }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowExactlyAsync<InvalidPasswordException>();
        manager.Verify(m => m.IsLockedOutAsync(user), Times.Once);
        manager.Verify(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PasswordTokensRequireSuccessfulSignInAndNoPendingSecondFactorAsync(bool twoFactor)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "user" };
        var manager = CreateConfiguredAuthManager(user, isLockedOut: false, twoFactorEnabled: twoFactor);
        var signIn = CreateSignIn(manager.Object);
        signIn.Setup(s => s.CheckPasswordSignInAsync(user, "password", lockoutOnFailure: true))
            .ReturnsAsync(twoFactor ? SignInResult.Success : SignInResult.Failed);

        await using var provider = CreateAuthServiceProvider(
            manager.Object,
            signIn.Object,
            Mock.Of<IUserRoleClaimStore<ApplicationUser>>(MockBehavior.Strict));
        var handler = new AuthPasswordRequestHandler(
            provider.GetRequiredService<IIdentityJwtAuthenticationService<ApplicationUser>>());

        await FluentActions.Awaiting(() =>
            handler.Handle(new AuthPasswordRequest { Username = nameof(user), Password = "password" }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowExactlyAsync<InvalidPasswordException>();
    }

    [Theory]
    [InlineData("password")]
    [InlineData("refresh_token")]
    public async Task StandardOAuthFormNamesReachTheHandlerAsync(string grant)
    {
        var passwordHandler = new Mock<IRequestHandler<AuthPasswordRequest, Token>>(MockBehavior.Strict);
        var refreshHandler = new Mock<IRequestHandler<AuthRefreshTokenRequest, Token>>(MockBehavior.Strict);
        if (string.Equals(grant, "password", StringComparison.Ordinal))
        {
            passwordHandler.Setup(h => h.Handle(
                    It.Is<AuthPasswordRequest>(r => r.Username == "user" && r.Password == "password"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Token { AccessToken = "synthetic-response" });
        }
        else
        {
            refreshHandler.Setup(h => h.Handle(
                    It.Is<AuthRefreshTokenRequest>(r => r.RefreshToken == "synthetic-refresh"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Token { AccessToken = "synthetic-response" });
        }

        using var server = CreateServer(passwordHandler.Object, refreshHandler.Object);
        using var client = server.GetTestClient();
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = grant,
            ["username"] = "user",
            ["password"] = "password",
            ["refresh_token"] = "synthetic-refresh",
        });
        using var response = await client.PostAsync("/api/token", form, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        passwordHandler.VerifyAll();
        refreshHandler.VerifyAll();
    }

    [Fact]
    public async Task UnsupportedGrantReturnsSerializableErrorAsync()
    {
        using var server = CreateServer(
            Mock.Of<IRequestHandler<AuthPasswordRequest, Token>>(MockBehavior.Strict),
            Mock.Of<IRequestHandler<AuthRefreshTokenRequest, Token>>(MockBehavior.Strict));
        using var client = server.GetTestClient();
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) { ["grant_type"] = "unknown" });
        using var response = await client.PostAsync("/api/token", form, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken)).Should().Contain("unsupported_grant_type");
    }

    private static Mock<UserManager<ApplicationUser>> CreateManager()
    {
        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        services.Setup(provider => provider.GetService(typeof(System.Diagnostics.Metrics.IMeterFactory))).Returns((object?)null);
        services.Setup(provider => provider.GetService(typeof(IPasskeyHandler<ApplicationUser>))).Returns((object?)null);
        var manager = new Mock<UserManager<ApplicationUser>>(
            MockBehavior.Strict,
            Mock.Of<IUserStore<ApplicationUser>>(MockBehavior.Strict),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services.Object,
            NullLogger<UserManager<ApplicationUser>>.Instance);
        manager.SetupAllProperties();
        manager.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        return manager;
    }

    private static Mock<UserManager<ApplicationUser>> CreateConfiguredAuthManager(
        ApplicationUser user,
        bool isLockedOut,
        bool twoFactorEnabled)
    {
        var manager = CreateManager();
        manager.SetupGet(m => m.SupportsUserLockout).Returns(value: true);
        manager.SetupGet(m => m.SupportsUserTwoFactor).Returns(value: true);
        manager.SetupGet(m => m.SupportsUserSecurityStamp).Returns(value: true);
        manager.SetupGet(m => m.SupportsUserEmail).Returns(value: false);
        manager.SetupGet(m => m.SupportsUserRole).Returns(value: true);
        manager.SetupGet(m => m.SupportsUserClaim).Returns(value: false);
        manager.Setup(m => m.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        manager.Setup(m => m.FindByIdAsync(user.Id.ToString("D"))).ReturnsAsync(user);
        manager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(() => user.Id.ToString("D"));
        manager.Setup(m => m.GetUserNameAsync(user)).ReturnsAsync(() => user.UserName);
        manager.Setup(m => m.GetSecurityStampAsync(user)).ReturnsAsync(() => user.SecurityStamp ?? string.Empty);
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(isLockedOut);
        manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(twoFactorEnabled);
        manager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        manager.Setup(m => m.GetAuthenticationTokenAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((string?)null);
        manager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync((string?)null);
        return manager;
    }

    private static ServiceProvider CreateAuthServiceProvider(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserRoleClaimStore<ApplicationUser> claimStore)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton(userManager);
        services.AddSingleton(signInManager);
        services.AddSingleton(claimStore);
        services.AddIdentityJwtApi<ApplicationUser>(options =>
        {
            options.Issuer = "IdentityWebApi";
            options.Audiences.Clear();
            options.Audiences.Add("IdentityWebApi");
            options.TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            options.SigningKeys.Add(
                new JwtSigningKeyDescriptor
                {
                    Kid = "default",
                    Algorithm = JwtKeyAlgorithm.HS256,
                    SymmetricKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
                    IsActiveSigningKey = true,
                });
        });
        return services.BuildServiceProvider();
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignIn(UserManager<ApplicationUser> manager)
    {
        var signIn = new Mock<SignInManager<ApplicationUser>>(
            MockBehavior.Strict,
            manager,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(MockBehavior.Strict),
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<ApplicationUser>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(MockBehavior.Strict),
            new DefaultUserConfirmation<ApplicationUser>());
        signIn.SetupAllProperties();
        return signIn;
    }

    private static TokenService CreateTokens() =>
        new(new TokenServiceOptions { SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) }, TimeProvider.System);

    private static IHost CreateServer(
        IRequestHandler<AuthPasswordRequest, Token> passwordHandler,
        IRequestHandler<AuthRefreshTokenRequest, Token> refreshHandler) =>
        new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers().AddApplicationPart(typeof(TokenController).Assembly);
                    services.AddSingleton<IWebApiMapper, WebApiMapper>();
                    services.AddSingleton<IRequestValidator<AuthPasswordRequest>, AuthPasswordRequestValidator>();
                    services.AddSingleton(passwordHandler);
                    services.AddSingleton(refreshHandler);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .Start();
}
