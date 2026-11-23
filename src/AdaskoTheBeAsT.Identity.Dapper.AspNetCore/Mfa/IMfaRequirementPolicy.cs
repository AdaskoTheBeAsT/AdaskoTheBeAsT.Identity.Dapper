using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public interface IMfaRequirementPolicy<in TUser>
    where TUser : class
{
    Task<bool> IsMfaRequiredAsync(
        TUser user,
        CancellationToken cancellationToken);
}
