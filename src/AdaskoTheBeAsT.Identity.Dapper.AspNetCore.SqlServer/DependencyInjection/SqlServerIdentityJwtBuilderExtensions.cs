using System;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Hosting;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Internal;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.DependencyInjection;

public static class SqlServerIdentityJwtBuilderExtensions
{
    public static IIdentityJwtBuilder<TUser> AddSqlServerJwtStores<TUser>(
        this IIdentityJwtBuilder<TUser> builder,
        Action<IdentityJwtSqlServerOptions> configureOptions)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        RegisterCommonSqlServerServices(builder.Services, configureOptions);
        builder.Services.Replace(ServiceDescriptor.Singleton<IRefreshTokenStore, SqlServerRefreshTokenStore>());
        builder.Services.Replace(ServiceDescriptor.Singleton<IMfaChallengeStore, SqlServerMfaChallengeStore>());
        builder.Services.Replace(ServiceDescriptor.Singleton<ITokenRevocationStore, SqlServerTokenRevocationStore>());
        builder.Services.Replace(ServiceDescriptor.Singleton<IJwtClaimCatalogStore, SqlServerJwtClaimCatalogStore>());

        return builder;
    }

    public static IIdentityJwtValidationBuilder AddSqlServerTokenRevocationStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtSqlServerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        RegisterCommonSqlServerServices(builder.Services, configureOptions);
        builder.Services.Replace(ServiceDescriptor.Singleton<ITokenRevocationStore, SqlServerTokenRevocationStore>());
        builder.Services.Replace(ServiceDescriptor.Singleton<IJwtClaimCatalogStore, SqlServerJwtClaimCatalogStore>());

        return builder;
    }

    public static IIdentityJwtValidationBuilder AddSqlServerClaimCatalogStore(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtSqlServerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        RegisterCommonSqlServerServices(builder.Services, configureOptions);
        builder.Services.Replace(ServiceDescriptor.Singleton<IJwtClaimCatalogStore, SqlServerJwtClaimCatalogStore>());

        return builder;
    }

    private static void RegisterCommonSqlServerServices(
        IServiceCollection services,
        Action<IdentityJwtSqlServerOptions> configureOptions)
    {
        services.AddOptions<IdentityJwtSqlServerOptions>()
            .Configure(configureOptions)
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<IdentityJwtSqlServerOptions>, IdentityJwtSqlServerOptionsValidator>());

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ISqlServerJwtConnectionFactory, SqlServerJwtConnectionFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, JwtStoreCleanupHostedService>());
    }
}
