using AdaskoTheBeAsT.AutoMapper.SimpleInjector;
using AdaskoTheBeAsT.FluentValidation.MediatR;
using AdaskoTheBeAsT.FluentValidation.SimpleInjector;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Persistence;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Validators;
using AdaskoTheBeAsT.MediatR.SimpleInjector.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleInjector;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddSingleton<IIdentityDbConnectionProvider<SqlConnection>>(_ => new IdentityDbConnectionProvider(connectionString));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IRoleStore<ApplicationRole>, ApplicationRoleStore>();
builder.Services.AddScoped<IUserStore<ApplicationUser>, ApplicationUserStore>();
builder.Services.AddMemoryCache();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

await using var container = new Container();
builder.Services.AddSimpleInjector(
    container,
    options =>
    {
        options.AddAspNetCore()
            .AddControllerActivation();
        options.AddLogging();
    });

container.AddAutoMapper(
    options => options.WithMapperAssemblyMarkerTypes(typeof(AuthenticationModel)));

container.AddFluentValidation(
    config =>
        config.WithAssemblyMarkerTypes(typeof(AuthPasswordRequestValidator)));

container.AddMediatRAspNetCore(
    config =>
    {
        config.WithHandlerAssemblyMarkerTypes(typeof(AuthPasswordRequestHandler));
        config.UsingPipelineProcessorBehaviors(typeof(FluentValidationPipelineBehavior<,>));
        config.UsingStreamPipelineBehaviors(typeof(FluentValidationStreamPipelineBehavior<,>));
    });

container.Register<IUserRoleClaimStore<ApplicationUser>, ApplicationUserStore>(Lifestyle.Scoped);
var tokenServiceOptions = builder.Configuration.GetSection(nameof(TokenServiceOptions)).Get<TokenServiceOptions>();
if (tokenServiceOptions != null)
{
    container.RegisterInstance(tokenServiceOptions);
}

container.Register<ITokenService, TokenService>(Lifestyle.Singleton);
container.Register<ITransactionScopeProvider, TransactionScopeProvider>(Lifestyle.Singleton);

var app = builder.Build();

app.Services.UseSimpleInjector(container);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

container.Verify();

container.GetInstance<AutoMapper.IConfigurationProvider>().AssertConfigurationIsValid();

await app.RunAsync().ConfigureAwait(continueOnCapturedContext: false);
