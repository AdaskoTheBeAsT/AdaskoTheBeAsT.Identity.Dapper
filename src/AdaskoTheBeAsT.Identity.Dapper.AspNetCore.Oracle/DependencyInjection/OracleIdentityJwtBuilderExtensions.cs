using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Oracle.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Oracle.Schema;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Oracle.DependencyInjection;

public static class OracleIdentityJwtBuilderExtensions
{
    public static IIdentityJwtBuilder<TUser> AddOracleJwtStores<TUser>(
        this IIdentityJwtBuilder<TUser> builder,
        Action<IdentityJwtOracleOptions> configure)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: true, revocations: true);
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddOracleTokenRevocationStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtOracleOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: false, revocations: true);
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddOracleClaimCatalogStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtOracleOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: false, revocations: false);
        return builder;
    }

    private static void Register(
        IServiceCollection services, Action<IdentityJwtOracleOptions> configure, bool issuer, bool revocations)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<IdentityJwtOracleOptions>()
            .Configure(configure)
            .Validate(options => !options.ToRelational().Validate().Any(), "Invalid Oracle Identity JWT store options.")
            .ValidateOnStart();
        services.TryAddSingleton(provider => OracleJwtSchemaHelper.CreateDatabase(
            provider.GetRequiredService<IOptions<IdentityJwtOracleOptions>>().Value,
            provider,
            provider.GetRequiredService<TimeProvider>()));
        RelationalRegistration.AddStores(services, issuer, revocations);
    }
}
