using System;
using Microsoft.Extensions.DependencyInjection;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;

internal sealed class IdentityJwtBuilder<TUser> : IIdentityJwtBuilder<TUser>
    where TUser : class
{
    public IdentityJwtBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    public IServiceCollection Services { get; }
}
