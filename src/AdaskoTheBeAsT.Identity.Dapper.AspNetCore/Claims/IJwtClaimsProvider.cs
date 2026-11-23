using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public interface IJwtClaimsProvider<in TUser>
    where TUser : class
{
    Task<IReadOnlyList<Claim>> GetClaimsAsync(
        TUser user,
        CancellationToken cancellationToken);
}
