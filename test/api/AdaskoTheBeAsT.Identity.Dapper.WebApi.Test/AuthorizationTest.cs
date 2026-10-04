using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;
using AutoMapper;
using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Test;

public sealed class AuthorizationTest : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthorizationTest(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("GET", "/api/user/11111111-1111-1111-1111-111111111111")]
    [InlineData("PUT", "/api/user/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/api/user/11111111-1111-1111-1111-111111111111")]
    [InlineData("GET", "/api/role")]
    [InlineData("POST", "/api/role")]
    public async Task AnonymousAdministrationIsRejected(string method, string path)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) };
        using var response = await client.SendAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousRegistrationCannotChooseRoles()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var response = await client.PostAsJsonAsync("/api/user", new UserModel { Roles = new[] { "Administrator" } }, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task ValidBearerCannotManageAnotherAccount(string method)
    {
        using var client = AuthenticatedClient(Guid.NewGuid());
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/user/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new UpdateUserModel { UserName = "changed" }),
        };
        using var response = await client.SendAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OwnerCannotChangeOwnRoles()
    {
        var id = Guid.NewGuid();
        using var client = AuthenticatedClient(id);
        using var response = await client.PutAsJsonAsync($"/api/user/{id}", new UpdateUserModel { Roles = new[] { "Administrator" } }, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OwnerReadReachesHandler()
    {
        var id = Guid.NewGuid();
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(m => m.Send(It.Is<GetUserByIdRequest>(r => r.UserId == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserModel { UserName = "owner" });
        var controller = new UserController(Mock.Of<IMapper>(MockBehavior.Strict), mediator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "test")),
                },
            },
        };
        (await controller.GetUserByIdAsync(id)).Should().BeOfType<OkObjectResult>();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task RegistrationHandlerRejectsPrivilegedRolesBeforePersistence()
    {
        var handler = new CreateUserRequestHandler(null!);
        var result = await handler.Handle(new CreateUserRequest { Roles = new[] { "Administrator" } }, CancellationToken.None);
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "RoleAssignmentNotAllowed");
    }

    private HttpClient AuthenticatedClient(Guid id)
    {
        using var service = new TokenService(new TokenServiceOptions { SigningKey = _factory.SigningKey }, TimeProvider.System);
        var token = service.GenerateToken(new ApplicationUser { Id = id, UserName = "owner" }, new List<string>(), new List<Claim>());
        new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken).Subject.Should().Be(id.ToString("D"));
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("TokenServiceOptions:SigningKey", SigningKey);
        builder.UseEnvironment("Testing");
    }
}
