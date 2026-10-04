using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

/// <summary>Reads a bounded role page in database ID order.</summary>
public interface IPagedRoleStore<TRole>
    where TRole : class
{
    Task<IList<TRole>> GetRolesPageAsync(int offset, int pageSize, CancellationToken cancellationToken = default);
}
