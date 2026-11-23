using Microsoft.Extensions.DependencyInjection;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;

#pragma warning disable S2326 // TUser is a DI marker type parameter for strongly-typed builder extensions
public interface IIdentityJwtBuilder<TUser>
    where TUser : class
{
    IServiceCollection Services { get; }
}
#pragma warning restore S2326
