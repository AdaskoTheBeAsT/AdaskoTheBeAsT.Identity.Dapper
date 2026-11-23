using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

internal interface IJwtUserRawClaimsLoader
{
    Task<IReadOnlyList<Claim>> LoadRawClaimsAsync(
        string userId,
        CancellationToken cancellationToken);
}
