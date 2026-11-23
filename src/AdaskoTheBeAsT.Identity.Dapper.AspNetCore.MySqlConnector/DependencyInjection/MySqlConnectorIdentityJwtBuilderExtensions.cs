using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySqlConnector.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySqlConnector.Schema;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySqlConnector.DependencyInjection;

public static class MySqlConnectorIdentityJwtBuilderExtensions
{
    public static IIdentityJwtBuilder<TUser> AddMySqlConnectorJwtStores<TUser>(
        this IIdentityJwtBuilder<TUser> builder,
        Action<IdentityJwtMySqlConnectorOptions> configure)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: true, revocations: true);
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddMySqlConnectorTokenRevocationStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtMySqlConnectorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: false, revocations: true);
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddMySqlConnectorClaimCatalogStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtMySqlConnectorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: false, revocations: false);
        return builder;
    }

    private static void Register(
        IServiceCollection services, Action<IdentityJwtMySqlConnectorOptions> configure, bool issuer, bool revocations)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<IdentityJwtMySqlConnectorOptions>()
            .Configure(configure)
            .Validate(options => !options.ToRelational().Validate().Any(), "Invalid MySqlConnector Identity JWT store options.")
            .ValidateOnStart();
        services.TryAddSingleton(provider => MySqlConnectorJwtSchemaHelper.CreateDatabase(
            provider.GetRequiredService<IOptions<IdentityJwtMySqlConnectorOptions>>().Value,
            provider,
            provider.GetRequiredService<TimeProvider>()));
        RelationalRegistration.AddStores(services, issuer, revocations);
    }
}
