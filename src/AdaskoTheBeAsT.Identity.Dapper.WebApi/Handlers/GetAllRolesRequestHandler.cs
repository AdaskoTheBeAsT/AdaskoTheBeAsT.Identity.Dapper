using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class GetAllRolesRequestHandler
    : IRequestHandler<GetAllRolesRequest, IEnumerable<RoleModel>>
{
    private readonly IPagedRoleStore<ApplicationRole> _roles;
    private readonly IWebApiMapper _mapper;

    public GetAllRolesRequestHandler(
        IPagedRoleStore<ApplicationRole> roles,
        IWebApiMapper mapper)
    {
        _roles = roles;
        _mapper = mapper;
    }

    public async Task<IEnumerable<RoleModel>> Handle(
        GetAllRolesRequest request,
        CancellationToken cancellationToken)
    {
        var roles = await _roles.GetRolesPageAsync(request.Offset, request.PageSize, cancellationToken).ConfigureAwait(false);
        return _mapper.MapToRoleModels(roles);
    }
}
