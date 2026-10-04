using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using AdaskoTheBeAsT.AutoMapper.SimpleInjector;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;
using AutoMapper;
using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SimpleInjector;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Test;

public sealed class AuthenticationSafetyTest
{
    [Fact]
    public async Task OmittedRolesSurviveActualMappingAndUpdate()
    {
        using var container = CreateMapperContainer();
        var mapper = container.GetInstance<IMapper>();
        var request = mapper.Map<UpdateUserRequest>(new UpdateUserModel { UserName = "renamed" });
        request.Roles.Should().BeNull();
        mapper.Map<UpdateUserRequest>(new UpdateUserModel { Roles = Array.Empty<string>() }).Roles!.Should().BeEmpty();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "old" };
        request.UserId = user.Id;
        var manager = CreateManager();
        manager.Setup(m => m.FindByIdAsync(user.Id.ToString("D"))).ReturnsAsync(user);
        manager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var result = await new UpdateUserRequestHandler(manager.Object).Handle(request, cancellationToken: TestContext.Current.CancellationToken);
        result.Succeeded.Should().BeTrue();
        manager.Verify(m => m.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        manager.Verify(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task RefreshFollowsImmutableIdAfterRenameAndRotates()
    {
        using var tokens = CreateTokens();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "recycled", SecurityStamp = "stamp" };
        var original = tokens.GenerateToken(user, new List<string>(), new List<Claim>());
        user.UserName = "renamed";
        var manager = CreateManager();
        manager.Setup(m => m.FindByIdAsync(user.Id.ToString("D"))).ReturnsAsync(user);
        manager.SetupGet(m => m.SupportsUserLockout).Returns(true);
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        manager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        var signIn = CreateSignIn(manager.Object);
        signIn.Setup(s => s.CanSignInAsync(user)).ReturnsAsync(true);
        var claims = new Mock<IUserRoleClaimStore<ApplicationUser>>(MockBehavior.Strict);
        claims.Setup(s => s.GetUserAndRoleClaimsAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Claim>());
        var handler = new AuthRefreshTokenRequestHandler(manager.Object, claims.Object, tokens, signIn.Object);
        var refreshed = await handler.Handle(new AuthRefreshTokenRequest { RefreshToken = original.RefreshToken }, TestContext.Current.CancellationToken);
        new JwtSecurityTokenHandler().ReadJwtToken(refreshed.AccessToken).Subject.Should().Be(user.Id.ToString("D"));
        refreshed.RefreshToken.Should().NotBe(original.RefreshToken);
        refreshed.UserName.Should().Be(user.UserName);
        await FluentActions.Awaiting(() =>
            handler.Handle(new AuthRefreshTokenRequest { RefreshToken = original.RefreshToken }, TestContext.Current.CancellationToken)).Should().ThrowExactlyAsync<InvalidRefreshTokenException>();
        manager.Verify(m => m.FindByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("deleted")]
    [InlineData("stamp")]
    [InlineData("locked")]
    [InlineData("not-allowed")]
    public async Task RefreshRejectsInvalidAccountState(string state)
    {
        using var tokens = CreateTokens();
        var user = new ApplicationUser { Id = Guid.NewGuid(), SecurityStamp = "original" };
        var issued = tokens.GenerateToken(user, new List<string>(), new List<Claim>());
        if (string.Equals(state, "stamp", StringComparison.Ordinal))
        {
            user.SecurityStamp = "changed";
        }

        var manager = CreateManager();
        manager.Setup(m => m.FindByIdAsync(user.Id.ToString("D"))).ReturnsAsync(string.Equals(state, "deleted", StringComparison.Ordinal) ? null : user);
        manager.SetupGet(m => m.SupportsUserLockout).Returns(true);
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(string.Equals(state, "locked", StringComparison.Ordinal));
        var signIn = CreateSignIn(manager.Object);
        signIn.Setup(s => s.CanSignInAsync(user)).ReturnsAsync(!string.Equals(state, "not-allowed", StringComparison.Ordinal));
        var handler = new AuthRefreshTokenRequestHandler(
            manager.Object,
            Mock.Of<IUserRoleClaimStore<ApplicationUser>>(MockBehavior.Strict),
            tokens,
            signIn.Object);
        await FluentActions.Awaiting(() =>
            handler.Handle(new AuthRefreshTokenRequest { RefreshToken = issued.RefreshToken }, TestContext.Current.CancellationToken)).Should().ThrowExactlyAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task ConcurrentRefreshConsumptionHasOnlyOneWinner()
    {
        using var tokens = CreateTokens();
        var issued = tokens.GenerateToken(new ApplicationUser { Id = Guid.NewGuid() }, new List<string>(), new List<Claim>());
        var outcomes = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
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
            })));
        outcomes.Should().ContainSingle(result => result);
    }

    [Fact]
    public async Task PasswordAuthenticationUsesFrameworkLockoutChecks()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), LockoutEnabled = true, LockoutEnd = DateTimeOffset.UtcNow.AddDays(1) };
        var manager = CreateManager();
        manager.Setup(m => m.FindByNameAsync("locked")).ReturnsAsync(user);
        manager.SetupGet(m => m.SupportsUserLockout).Returns(true);
        manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);
        var signIn = new SignInManager<ApplicationUser>(
            manager.Object,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(MockBehavior.Strict),
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<ApplicationUser>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(MockBehavior.Strict),
            new DefaultUserConfirmation<ApplicationUser>());
        var handler = new AuthPasswordRequestHandler(
            manager.Object,
            Mock.Of<IUserRoleClaimStore<ApplicationUser>>(MockBehavior.Strict),
            Mock.Of<ITokenService>(MockBehavior.Strict),
            signIn);
        await FluentActions.Awaiting(() =>
            handler.Handle(new AuthPasswordRequest { Username = "locked", Password = "not-evaluated" }, TestContext.Current.CancellationToken)).Should().ThrowExactlyAsync<InvalidPasswordException>();
        manager.Verify(m => m.IsLockedOutAsync(user), Times.Once);
        manager.Verify(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task PasswordTokensRequireSuccessfulSignInAndNoPendingSecondFactor(bool twoFactor)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid() };
        var manager = CreateManager();
        manager.Setup(m => m.FindByNameAsync(nameof(user))).ReturnsAsync(user);
        manager.SetupGet(m => m.SupportsUserTwoFactor).Returns(true);
        manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(twoFactor);
        var signIn = CreateSignIn(manager.Object);
        signIn.Setup(s => s.CheckPasswordSignInAsync(user, "password", true))
            .ReturnsAsync(twoFactor ? SignInResult.Success : SignInResult.Failed);
        var handler = new AuthPasswordRequestHandler(
            manager.Object,
            Mock.Of<IUserRoleClaimStore<ApplicationUser>>(MockBehavior.Strict),
            Mock.Of<ITokenService>(MockBehavior.Strict),
            signIn.Object);
        return FluentActions.Awaiting(() =>
            handler.Handle(new AuthPasswordRequest { Username = nameof(user), Password = "password" }, TestContext.Current.CancellationToken)).Should().ThrowExactlyAsync<InvalidPasswordException>();
    }

    [Theory]
    [InlineData("password")]
    [InlineData("refresh_token")]
    public async Task StandardOAuthFormNamesReachTheHandler(string grant)
    {
        using var container = CreateMapperContainer();
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(m => m.Send(
            It.Is<IRequest<Token>>(r =>
                grant == "password" ? r is AuthPasswordRequest : r is AuthRefreshTokenRequest &&
                    ((AuthRefreshTokenRequest)r).RefreshToken == "synthetic-refresh"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Token { AccessToken = "synthetic-response" });
        using var server = CreateServer(container.GetInstance<IMapper>(), mediator.Object);
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
        mediator.VerifyAll();
    }

    [Fact]
    public async Task UnsupportedGrantReturnsSerializableError()
    {
        using var container = CreateMapperContainer();
        using var server = CreateServer(container.GetInstance<IMapper>(), Mock.Of<IMediator>(MockBehavior.Strict));
        using var client = server.GetTestClient();
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) { ["grant_type"] = "unknown" });
        using var response = await client.PostAsync("/api/token", form, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken)).Should().Contain("unsupported_grant_type");
    }

    private static Container CreateMapperContainer()
    {
        var container = new Container();
        container.RegisterInstance<ILoggerFactory>(NullLoggerFactory.Instance);
        container.AddAutoMapper(o => o.WithMapperAssemblyMarkerTypes(typeof(AuthenticationModel)));
        return container;
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
        return manager;
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

    private static IHost CreateServer(IMapper mapper, IMediator mediator) => new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers().AddApplicationPart(typeof(TokenController).Assembly);
                    services.AddSingleton(mapper);
                    services.AddSingleton(mediator);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                })).Start();
}
