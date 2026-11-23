using System;
using Microsoft.Extensions.DependencyInjection;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;

internal sealed class IdentityJwtValidationBuilder : IIdentityJwtValidationBuilder
{
    public IdentityJwtValidationBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    public IServiceCollection Services { get; }
}
