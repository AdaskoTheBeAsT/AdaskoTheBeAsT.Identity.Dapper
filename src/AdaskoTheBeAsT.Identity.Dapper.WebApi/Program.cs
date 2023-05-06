using System.Text.Json;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.MediatR.SimpleInjector.AspNetCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleInjector;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
builder.Services.AddSingleton<IIdentityDbConnectionProvider>(_ => new IdentityDbConnectionProvider(connectionString));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IRoleStore<ApplicationRole>, ApplicationRoleStore>();
builder.Services.AddScoped<IUserStore<ApplicationUser>, ApplicationUserStore>();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

using var container = new Container();
builder.Services.AddSimpleInjector(
    container,
    options =>
    {
        options.AddAspNetCore()
            .AddControllerActivation();
        options.AddLogging();
    });

container.AddMediatRAspNetCore(
    config =>
    {
        config.WithHandlerAssemblyMarkerTypes(typeof(AuthPasswordRequestHandler));
    });

container.Register<IUserRoleClaimStore<ApplicationUser>, ApplicationUserStore>(Lifestyle.Scoped);

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

app.MapGet(
    "antiforgery/token",
    (
        IAntiforgery forgeryService,
        HttpContext context) =>
    {
        var tokens = forgeryService.GetAndStoreTokens(context);
#pragma warning disable SCS0008, SCS0009
        context.Response.Cookies.Append(
            "XSRF-TOKEN",
            tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,
            });
#pragma warning restore SCS0008, SCS0009

        return Results.Ok();
    })
    .RequireAuthorization();

container.Verify();

app.Run();
