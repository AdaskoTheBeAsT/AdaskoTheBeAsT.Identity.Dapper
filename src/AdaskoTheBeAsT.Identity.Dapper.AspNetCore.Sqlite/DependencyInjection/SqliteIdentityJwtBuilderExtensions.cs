using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.DependencyInjection;

public static class SqliteIdentityJwtBuilderExtensions
{
    public static IIdentityJwtBuilder<TUser> AddSqliteJwtStores<TUser>(
        this IIdentityJwtBuilder<TUser> builder,
        Action<IdentityJwtSqliteOptions> configure)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: true, revocations: true);
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddSqliteTokenRevocationStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtSqliteOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: false, revocations: true);
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddSqliteClaimCatalogStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtSqliteOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Register(builder.Services, configure, issuer: false, revocations: false);
        return builder;
    }

    private static void Register(
        IServiceCollection services, Action<IdentityJwtSqliteOptions> configure, bool issuer, bool revocations)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<IdentityJwtSqliteOptions>()
            .Configure(configure)
            .Validate(options => !options.ToRelational().Validate().Any(), "Invalid Sqlite Identity JWT store options.")
            .ValidateOnStart();
        services.TryAddSingleton(provider => SqliteJwtSchemaHelper.CreateDatabase(
            provider.GetRequiredService<IOptions<IdentityJwtSqliteOptions>>().Value,
            provider,
            provider.GetRequiredService<TimeProvider>()));
        RelationalRegistration.AddStores(services, issuer, revocations);
    }
}
