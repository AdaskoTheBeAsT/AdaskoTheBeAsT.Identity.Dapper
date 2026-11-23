using Microsoft.Extensions.DependencyInjection;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;

public interface IIdentityJwtValidationBuilder
{
    IServiceCollection Services { get; }
}
