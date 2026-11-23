using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Endpoints;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Persistence;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddSingleton<IIdentityDbConnectionProvider<SqlConnection>>(
    _ => new IdentityDbConnectionProvider(connectionString));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddRoleStore<ApplicationRoleStore>()
    .AddUserStore<ApplicationUserStore>()
    .AddDefaultTokenProviders();

var tokenServiceOptions = builder.Configuration.GetSection(nameof(TokenServiceOptions)).Get<TokenServiceOptions>()
    ?? throw new InvalidOperationException("Configure TokenServiceOptions:SigningKey before starting the example.");
if (string.IsNullOrWhiteSpace(tokenServiceOptions.SigningKey) ||
    Encoding.UTF8.GetByteCount(tokenServiceOptions.SigningKey) < 32)
{
    throw new InvalidOperationException("TokenServiceOptions:SigningKey must contain at least 32 UTF-8 bytes.");
}

// Recognize the public demo key by its fingerprint without duplicating its value.
var signingKeyBytes = Encoding.UTF8.GetBytes(tokenServiceOptions.SigningKey);
if (!builder.Environment.IsDevelopment() &&
    string.Equals(
        Convert.ToHexString(SHA256.HashData(signingKeyBytes)),
        IdentityJwtOptions.KnownDemoSigningKeySha256Hex,
        StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        "The public demo signing key is only allowed in Development. Configure a private TokenServiceOptions:SigningKey.");
}

var derivedTokenHashKey = Convert.ToBase64String(
    HKDF.DeriveKey(
        HashAlgorithmName.SHA256,
        signingKeyBytes,
        outputLength: 32,
        info: "IdentityJwt.TokenHashKey.v1"u8.ToArray()));

var jwtBuilder = builder.Services.AddIdentityJwtApi<ApplicationUser>(options =>
{
    builder.Configuration.GetSection("IdentityJwt").Bind(options);
    if (string.IsNullOrWhiteSpace(options.Issuer) ||
        string.Equals(options.Issuer, "IdentityIssuer", StringComparison.Ordinal))
    {
        options.Issuer = "IdentityWebApi";
    }

    if (options.Audiences.Count == 0)
    {
        options.Audiences.Add("IdentityWebApi");
    }

    if (options.SigningKeys.Count == 0)
    {
        options.SigningKeys.Add(
            new JwtSigningKeyDescriptor
            {
                Kid = "default",
                Algorithm = JwtKeyAlgorithm.HS256,
                SymmetricKey = tokenServiceOptions.SigningKey,
                IsActiveSigningKey = true,
            });
    }

    if (string.IsNullOrWhiteSpace(options.TokenHashKey))
    {
        options.TokenHashKey = derivedTokenHashKey;
    }

    options.InMemoryRefreshTokenCapacity = tokenServiceOptions.RefreshTokenCapacity;
});

if (builder.Configuration.GetValue<bool>("IdentityJwt:UseSqlServerStores") &&
    !string.IsNullOrWhiteSpace(connectionString))
{
    jwtBuilder.AddSqlServerJwtStores(sql => sql.ConnectionString = connectionString);
}

builder.Services.AddMemoryCache();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IUserRoleClaimStore<ApplicationUser>, ApplicationUserStore>();
builder.Services.AddScoped<IPagedRoleStore<ApplicationRole>, ApplicationRoleStore>();
builder.Services.AddSingleton(tokenServiceOptions);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<ITransactionScopeProvider, TransactionScopeProvider>();
builder.Services.AddSingleton<IWebApiMapper, WebApiMapper>();
builder.Services.AddSingleton<IRequestValidator<AuthPasswordRequest>, AuthPasswordRequestValidator>();

builder.Services.AddScoped<IRequestHandler<AuthPasswordRequest, Token>, AuthPasswordRequestHandler>();
builder.Services.AddScoped<IRequestHandler<AuthRefreshTokenRequest, Token>, AuthRefreshTokenRequestHandler>();
builder.Services.AddScoped<IRequestHandler<CreateUserRequest, IdentityResult>, CreateUserRequestHandler>();
builder.Services.AddScoped<IRequestHandler<GetUserByIdRequest, UserModel?>, GetUserByIdRequestHandler>();
builder.Services.AddScoped<IRequestHandler<UpdateUserRequest, IdentityResult>, UpdateUserRequestHandler>();
builder.Services.AddScoped<IRequestHandler<DeleteUserRequest, IdentityResult>, DeleteUserRequestHandler>();
builder.Services.AddScoped<IRequestHandler<CreateRoleRequest, IdentityResult>, CreateRoleRequestHandler>();
builder.Services.AddScoped<IRequestHandler<GetAllRolesRequest, IEnumerable<RoleModel>>, GetAllRolesRequestHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/auth")
    .MapIdentityJwtApi<ApplicationUser>();

app.MapControllers();

await app.RunAsync().ConfigureAwait(continueOnCapturedContext: false);
