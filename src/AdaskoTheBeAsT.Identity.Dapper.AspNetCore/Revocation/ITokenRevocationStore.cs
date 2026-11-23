using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;

public interface ITokenRevocationStore
{
    Task RevokeAsync(
        TokenRevocationEntry entry,
        CancellationToken cancellationToken);

    Task<bool> IsRevokedAsync(
        TokenRevocationCheckContext context,
        CancellationToken cancellationToken);

    Task<int> PruneExpiredAsync(
        int batchSize,
        CancellationToken cancellationToken);
}
