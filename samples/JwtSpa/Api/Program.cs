using System.Security.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Authorization;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Endpoints;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.Schema;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite;
using Dapper;
using JwtSpa.Api;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("This sample enables public registration. Run it locally in Development only.");
}

SQLitePCL.Batteries.Init();
SqliteDapperConfig.ConfigureTypeHandlers();

const string connectionString = "Data Source=jwt-spa;Mode=Memory;Cache=Shared;Pooling=False;Foreign Keys=True";
var keeper = new SqliteConnection(connectionString);
await using var disposeKeeper = keeper.ConfigureAwait(false);
await keeper.OpenAsync().ConfigureAwait(false);
using var script = typeof(ApplicationUser).Assembly.GetManifestResourceStream("Identity.sql")
    ?? throw new InvalidOperationException("The embedded SQLite Identity schema was not found.");
using var reader = new StreamReader(script);
await keeper.ExecuteAsync(await reader.ReadToEndAsync().ConfigureAwait(false)).ConfigureAwait(false);
await SqliteJwtSchemaHelper.EnsureSchemaAndTablesAsync(new() { ConnectionString = connectionString }).ConfigureAwait(false);

builder.Services.AddSingleton<IIdentityDbConnectionProvider<SqliteConnection>>(new SampleConnectionProvider(connectionString));
builder.Services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
    .AddRoles<ApplicationRole>()
    .AddUserStore<ApplicationUserStore>()
    .AddRoleStore<ApplicationRoleStore>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
var privatePem = signingKey.ExportECPrivateKeyPem();
builder.Services.AddIdentityJwtApi<ApplicationUser>(options =>
    {
        options.Issuer = "jwt-spa-sample";
        options.Audiences.Add("jwt-spa");
        options.TokenHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        options.SigningKeys.Add(new JwtSigningKeyDescriptor
        {
            Kid = "local",
            Algorithm = JwtKeyAlgorithm.ES256,
            Pem = privatePem,
        });
        options.Mfa.AuthenticatorIssuer = "Identity Dapper Demo";
        options.Mfa.AllowEmailMfa = false;
        options.Mfa.EnforcementMode = builder.Configuration.GetValue<bool>("Sample:RequireMfa")
            ? MfaEnforcementMode.RequiredForAllUsers
            : MfaEnforcementMode.PerUserSetting;
    })
    .AddSqliteJwtStores(options => options.ConnectionString = connectionString)
    .AddMfaQrCodeSvg();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("step-up", policy => policy.RequireAuthenticatedUser().RequireMfaAssurance());

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGroup("/api/auth").MapIdentityJwtApi<ApplicationUser>(options => options.EnableRegistration = true);
app.MapGet("/api/private", () => Results.Ok(new { Message = "The API accepted your access token." })).RequireAuthorization();
app.MapGet("/api/step-up", () => Results.Ok(new { Message = "The API verified MFA assurance." })).RequireAuthorization("step-up");
#pragma warning disable S1075 // The development proxies expect a fixed loopback-only endpoint.
await app.RunAsync("http://127.0.0.1:5080").ConfigureAwait(false);
#pragma warning restore S1075
