using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal static class RelationalRegistration
{
    public static void AddStores(IServiceCollection services, bool issuer, bool revocations)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.Replace(ServiceDescriptor.Singleton<IJwtClaimCatalogStore, RelationalClaimCatalog>());
        if (revocations)
        {
            services.Replace(ServiceDescriptor.Singleton<ITokenRevocationStore, RelationalRevocationStore>());
        }

        if (issuer)
        {
            services.Replace(ServiceDescriptor.Singleton<IRefreshTokenStore, RelationalRefreshStore>());
            services.Replace(ServiceDescriptor.Singleton<IMfaChallengeStore, RelationalMfaStore>());
        }

        if (revocations)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RelationalCleanup>());
        }
    }
}
