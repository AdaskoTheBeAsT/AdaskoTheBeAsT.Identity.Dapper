using System;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;

public static class IdentityJwtValidationServiceCollectionExtensions
{
    public static IIdentityJwtValidationBuilder AddIdentityJwtValidation(
        this IServiceCollection services,
        Action<IdentityJwtValidationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<IdentityJwtValidationOptions>()
            .Configure(configure)
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<IdentityJwtValidationOptions>, IdentityJwtValidationOptionsValidator>());

        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IJwtValidationKeysProvider, DefaultJwtValidationKeysProvider>();
        services.TryAddSingleton<ITokenRevocationStore, InMemoryTokenRevocationStore>();
        services.TryAddSingleton<IJwtClaimCatalogStore, InMemoryJwtClaimCatalogStore>();
        services.TryAddSingleton<IJwtClaimTransformationCache, JwtClaimTransformationCache>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerValidationPostConfigureOptions>());

        services.AddProblemDetails();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.Replace(ServiceDescriptor.Scoped<IClaimsTransformation, JwtClaimsTransformation>());
        services.AddAuthorization();

        return new IdentityJwtValidationBuilder(services);
    }

    public static IIdentityJwtValidationBuilder AddClaimKindTransformation(
        this IIdentityJwtValidationBuilder builder,
        Action<IdentityJwtClaimKindOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<IdentityJwtValidationOptions>()
            .Configure(options =>
            {
                options.ClaimKinds.EnableClaimsTransformation = true;
                configure?.Invoke(options.ClaimKinds);
            });

        builder.Services.AddMemoryCache();
        builder.Services.TryAddSingleton<IJwtClaimCatalogStore, InMemoryJwtClaimCatalogStore>();
        builder.Services.TryAddSingleton<IJwtClaimTransformationCache, JwtClaimTransformationCache>();
        builder.Services.Replace(ServiceDescriptor.Scoped<IClaimsTransformation, JwtClaimsTransformation>());
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddClaimCatalogStore<TStore>(
        this IIdentityJwtValidationBuilder builder)
        where TStore : class, IJwtClaimCatalogStore
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Replace(ServiceDescriptor.Scoped<IJwtClaimCatalogStore, TStore>());
        return builder;
    }

    public static IIdentityJwtValidationBuilder AddDistributedCacheTokenRevocationStore(
        this IIdentityJwtValidationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Replace(
            ServiceDescriptor.Singleton<ITokenRevocationStore, DistributedCacheTokenRevocationStore>());
        builder.Services.AddOptions<IdentityJwtValidationOptions>()
            .Configure(options => options.ClaimKinds.UseDistributedCache = true);
        return builder;
    }
}
