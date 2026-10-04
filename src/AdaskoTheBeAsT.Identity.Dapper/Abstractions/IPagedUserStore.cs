using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

/// <summary>Reads a bounded user page in database ID order.</summary>
public interface IPagedUserStore<TUser>
    where TUser : class
{
    Task<IList<TUser>> GetUsersPageAsync(int offset, int pageSize, CancellationToken cancellationToken = default);
}
