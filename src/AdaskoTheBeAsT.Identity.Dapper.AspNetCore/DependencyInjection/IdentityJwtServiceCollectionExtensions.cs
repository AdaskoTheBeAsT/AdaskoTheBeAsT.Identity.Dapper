using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Services;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;

public static class IdentityJwtServiceCollectionExtensions
{
    public static IIdentityJwtBuilder<TUser> AddIdentityJwtApi<TUser>(
        this IServiceCollection services,
        Action<IdentityJwtOptions> configure)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<IdentityJwtOptions>()
            .Configure(configure)
            .ValidateOnStart();

        services.AddOptions<IdentityJwtValidationOptions>()
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<IdentityJwtOptions>, IdentityJwtOptionsValidator>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureOptions<IdentityJwtValidationOptions>, SyncValidationFromIssuerPostConfigureOptions>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<IdentityJwtValidationOptions>, IdentityJwtValidationOptionsValidator>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerValidationPostConfigureOptions>());

        RegisterCoreStoresAndServices<TUser>(services);

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

        return new IdentityJwtBuilder<TUser>(services);
    }

    public static IIdentityJwtBuilder<TUser> AddClaimKindSupport<TUser>(
        this IIdentityJwtBuilder<TUser> builder,
        Action<IdentityJwtClaimKindOptions>? configure = null)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<IdentityJwtOptions>()
            .Configure(options =>
            {
                options.ClaimKinds.FilterAccessTokenClaimsByKind = true;
                options.ClaimKinds.EnableClaimsTransformation = true;
                configure?.Invoke(options.ClaimKinds);
            });

        builder.Services.AddMemoryCache();
        builder.Services.TryAddSingleton<IJwtClaimCatalogStore, InMemoryJwtClaimCatalogStore>();
        builder.Services.TryAddSingleton<IJwtClaimTransformationCache, JwtClaimTransformationCache>();
        builder.Services.Replace(ServiceDescriptor.Scoped<IClaimsTransformation, JwtClaimsTransformation>());
        return builder;
    }

    public static IIdentityJwtBuilder<TUser> AddClaimCatalogStore<TUser, TStore>(
        this IIdentityJwtBuilder<TUser> builder)
        where TUser : class
        where TStore : class, IJwtClaimCatalogStore
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Replace(ServiceDescriptor.Scoped<IJwtClaimCatalogStore, TStore>());
        return builder;
    }

    public static IIdentityJwtBuilder<TUser> AddClaimsProvider<TUser, TProvider>(
        this IIdentityJwtBuilder<TUser> builder)
        where TUser : class
        where TProvider : class, IJwtClaimsProvider<TUser>
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Replace(ServiceDescriptor.Scoped<IJwtClaimsProvider<TUser>, TProvider>());
        return builder;
    }

    public static IIdentityJwtBuilder<TUser> AddClaimsProvider<TUser>(
        this IIdentityJwtBuilder<TUser> builder,
        Func<TUser, CancellationToken, Task<IReadOnlyList<Claim>>> enricher)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(enricher);
        builder.Services.Replace(
            ServiceDescriptor.Scoped<IJwtClaimsProvider<TUser>>(sp =>
                new DelegateJwtClaimsProvider<TUser>(
                    sp.GetRequiredService<DefaultJwtClaimsProvider<TUser>>(),
                    enricher)));
        return builder;
    }

    public static IIdentityJwtBuilder<TUser> AddMfaCodeSender<TUser, TSender>(
        this IIdentityJwtBuilder<TUser> builder)
        where TUser : class
        where TSender : class, IMfaCodeSender<TUser>
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Replace(ServiceDescriptor.Scoped<IMfaCodeSender<TUser>, TSender>());
        return builder;
    }

    public static IIdentityJwtBuilder<TUser> AddMfaRequirementPolicy<TUser, TPolicy>(
        this IIdentityJwtBuilder<TUser> builder)
        where TUser : class
        where TPolicy : class, IMfaRequirementPolicy<TUser>
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Replace(ServiceDescriptor.Scoped<IMfaRequirementPolicy<TUser>, TPolicy>());
        return builder;
    }

    public static IIdentityJwtBuilder<TUser> AddMfaQrCodeGenerator<TUser, TGenerator>(
        this IIdentityJwtBuilder<TUser> builder)
        where TUser : class
        where TGenerator : class, IMfaQrCodeGenerator
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Replace(ServiceDescriptor.Singleton<IMfaQrCodeGenerator, TGenerator>());
        return builder;
    }

    public static IIdentityJwtBuilder<TUser> AddMfaQrCodeSvg<TUser>(
        this IIdentityJwtBuilder<TUser> builder)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddSingleton<IMfaQrCodeGenerator, QrCoderMfaQrCodeGenerator>();
        return builder;
    }

    public static IIdentityJwtBuilder<TUser> AddDistributedCacheStores<TUser, TMfaCache>(
        this IIdentityJwtBuilder<TUser> builder)
        where TUser : class
        where TMfaCache : class, IAtomicMfaChallengeCache
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Replace(ServiceDescriptor.Singleton<ITokenRevocationStore, DistributedCacheTokenRevocationStore>());
        builder.Services.Replace(ServiceDescriptor.Singleton<IAtomicMfaChallengeCache, TMfaCache>());
        builder.Services.Replace(ServiceDescriptor.Singleton<IMfaChallengeStore, DistributedCacheMfaChallengeStore>());
        builder.Services.AddOptions<IdentityJwtOptions>()
            .Configure(options => options.ClaimKinds.UseDistributedCache = true);
        return builder;
    }

    private static void RegisterCoreStoresAndServices<TUser>(IServiceCollection services)
        where TUser : class
    {
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ITokenSecretHasher, HmacTokenSecretHasher>();
        services.TryAddSingleton<IJwtSigningCredentialsProvider, DefaultJwtSigningCredentialsProvider>();
        services.TryAddSingleton<IJwtValidationKeysProvider, DefaultJwtValidationKeysProvider>();
        services.TryAddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.TryAddSingleton<ITokenRevocationStore, InMemoryTokenRevocationStore>();
        services.TryAddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
        services.TryAddSingleton<IMfaChallengeStore, InMemoryMfaChallengeStore>();
        services.TryAddSingleton<IJwtUserRawClaimsLoader, IdentityUserRawClaimsLoader<TUser>>();
        services.TryAddSingleton<IJwtClaimCatalogStore, InMemoryJwtClaimCatalogStore>();
        services.TryAddSingleton<IJwtClaimTransformationCache, JwtClaimTransformationCache>();

        services.TryAddScoped<DefaultJwtClaimsProvider<TUser>>();
        services.TryAddScoped<IJwtClaimsProvider<TUser>>(sp => sp.GetRequiredService<DefaultJwtClaimsProvider<TUser>>());
        services.TryAddScoped<IMfaRequirementPolicy<TUser>, DefaultMfaRequirementPolicy<TUser>>();
        services.TryAddScoped<IMfaCodeSender<TUser>, LoggingMfaCodeSender<TUser>>();
        services.TryAddScoped<MfaUserTokenManager<TUser>>();
        services.TryAddScoped<JwtSessionIssuer<TUser>>();
        services.TryAddScoped<IIdentityJwtMfaService<TUser>, IdentityJwtMfaService<TUser>>();
        services.TryAddScoped<IIdentityJwtAuthenticationService<TUser>, IdentityJwtAuthenticationService<TUser>>();
    }
}
