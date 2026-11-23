using System.Security.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Authorization;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Endpoints;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;

internal sealed class JwtTestHostHarness : IAsyncDisposable
{
    public const string DefaultIssuer = "https://auth.example.test";
    public const string DefaultAudience = "https://api.example.test";

    private readonly WebApplication _app;

    private JwtTestHostHarness(
        WebApplication app,
        HttpClient client,
        InMemoryUserStore userStore,
        AdjustableTimeProvider timeProvider,
        CapturingMfaCodeSender codeSender)
    {
        _app = app;
        Client = client;
        UserStore = userStore;
        TimeProvider = timeProvider;
        CodeSender = codeSender;
    }

    public HttpClient Client { get; }

    public InMemoryUserStore UserStore { get; }

    public AdjustableTimeProvider TimeProvider { get; }

    public CapturingMfaCodeSender CodeSender { get; }

    public IServiceProvider Services => _app.Services;

    public static async Task<JwtTestHostHarness> CreateAsync(
        Action<IdentityJwtOptions>? configureOptions = null,
        Action<IdentityJwtEndpointOptions>? configureEndpoints = null,
        Action<IIdentityJwtBuilder<TestUser>>? configureBuilder = null,
        Action<IServiceCollection>? configureServices = null,
        string environmentName = "Production",
        Action<WebApplication>? configureApp = null)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privatePem = ecdsa.ExportECPrivateKeyPem();
        var tokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var userStore = new InMemoryUserStore();
        var timeProvider = new AdjustableTimeProvider();
        var codeSender = new CapturingMfaCodeSender();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName,
        });
        builder.WebHost.UseTestServer();

        builder.Services.AddDataProtection();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Services.AddSingleton(userStore);
        builder.Services.AddSingleton<IUserStore<TestUser>>(userStore);
        builder.Services.AddSingleton<IUserRoleClaimStore<TestUser>>(userStore);

        ConfigureIdentity(builder.Services);

        var jwtBuilder = builder.Services.AddIdentityJwtApi<TestUser>(options =>
        {
            options.Issuer = DefaultIssuer;
            options.Audiences.Add(DefaultAudience);
            options.TokenHashKey = tokenHashKey;
            options.SigningKeys.Add(new JwtSigningKeyDescriptor
            {
                Kid = "ec-primary",
                Algorithm = JwtKeyAlgorithm.ES256,
                Pem = privatePem,
                IsActiveSigningKey = true,
            });
            options.Mfa.PublicSetupBaseUri = new Uri("https://portal.example.test/security/mfa-setup");
            configureOptions?.Invoke(options);
        });

        builder.Services.Replace(ServiceDescriptor.Singleton<IMfaCodeSender<TestUser>>(codeSender));
        configureBuilder?.Invoke(jwtBuilder);
        configureServices?.Invoke(builder.Services);

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("MfaRequired", p => p.RequireMfaAssurance());

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGroup("/auth").MapIdentityJwtApi<TestUser>(configureEndpoints);
        MapTestResourceEndpoints(app);
        configureApp?.Invoke(app);

        await app.StartAsync().ConfigureAwait(false);
        var client = app.GetTestClient();
        return new JwtTestHostHarness(app, client, userStore, timeProvider, codeSender);
    }

    public async Task<TestUser> SeedUserAsync(
        string userName,
        string password,
        string? email = null,
        bool emailConfirmed = true)
    {
        using var scope = _app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TestUser>>();
        var user = new TestUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email ?? $"{userName}@example.test",
            EmailConfirmed = emailConfirmed,
        };
        var result = await userManager.CreateAsync(user, password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync().ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
        UserStore.Dispose();
    }

    private static void ConfigureIdentity(IServiceCollection services)
    {
        services
            .AddIdentityCore<TestUser>(o =>
            {
                o.Password.RequireDigit = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireLowercase = false;
                o.Password.RequiredLength = 6;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddSignInManager();
    }

    private static void MapTestResourceEndpoints(WebApplication app)
    {
        app.MapGet(
                "/resource/protected",
                (HttpContext ctx) => Results.Ok(new
                {
                    Authenticated = ctx.User.Identity?.IsAuthenticated ?? false,
                    Claims = ctx.User.Claims.Select(c => new { c.Type, c.Value }).ToArray(),
                }))
            .RequireAuthorization();

        app.MapGet(
                "/resource/mfa-only",
                () => Results.Ok(new { Status = "mfa-verified" }))
            .RequireAuthorization("MfaRequired");
    }
}
