using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySql.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySql.Schema;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySql.DependencyInjection;

public static class MySqlIdentityJwtBuilderExtensions
{
    public static IIdentityJwtBuilder<TUser> AddMySqlJwtStores<TUser>(
        this IIdentityJwtBuilder<TUser> builder,
        Action<IdentityJwtMySqlOptions> configure)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: true, revocations: true);
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddMySqlTokenRevocationStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtMySqlOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: false, revocations: true);
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddMySqlClaimCatalogStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtMySqlOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: false, revocations: false);
        return builder;
    }

    private static void Register(
        IServiceCollection services, Action<IdentityJwtMySqlOptions> configure, bool issuer, bool revocations)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<IdentityJwtMySqlOptions>()
            .Configure(configure)
            .Validate(options => !options.ToRelational().Validate().Any(), "Invalid MySql Identity JWT store options.")
            .ValidateOnStart();
        services.TryAddSingleton(provider => MySqlJwtSchemaHelper.CreateDatabase(
            provider.GetRequiredService<IOptions<IdentityJwtMySqlOptions>>().Value,
            provider,
            provider.GetRequiredService<TimeProvider>()));
        RelationalRegistration.AddStores(services, issuer, revocations);
    }
}
