using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.Schema;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.DependencyInjection;

public static class PostgreSqlIdentityJwtBuilderExtensions
{
    public static IIdentityJwtBuilder<TUser> AddPostgreSqlJwtStores<TUser>(
        this IIdentityJwtBuilder<TUser> builder,
        Action<IdentityJwtPostgreSqlOptions> configure)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: true, revocations: true);
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddPostgreSqlTokenRevocationStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtPostgreSqlOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: false, revocations: true);
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddPostgreSqlClaimCatalogStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtPostgreSqlOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: false, revocations: false);
        return builder;
    }

    private static void Register(
        IServiceCollection services, Action<IdentityJwtPostgreSqlOptions> configure, bool issuer, bool revocations)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<IdentityJwtPostgreSqlOptions>()
            .Configure(configure)
            .Validate(options => !options.ToRelational().Validate().Any(), "Invalid PostgreSql Identity JWT store options.")
            .ValidateOnStart();
        services.TryAddSingleton(provider => PostgreSqlJwtSchemaHelper.CreateDatabase(
            provider.GetRequiredService<IOptions<IdentityJwtPostgreSqlOptions>>().Value,
            provider,
            provider.GetRequiredService<TimeProvider>()));
        RelationalRegistration.AddStores(services, issuer, revocations);
    }
}
