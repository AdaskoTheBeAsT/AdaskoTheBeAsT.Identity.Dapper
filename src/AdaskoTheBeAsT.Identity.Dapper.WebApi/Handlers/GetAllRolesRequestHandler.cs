using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class GetAllRolesRequestHandler
    : IRequestHandler<GetAllRolesRequest, IEnumerable<RoleModel>>
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IMapper _mapper;

    public GetAllRolesRequestHandler(RoleManager<ApplicationRole> roleManager, IMapper mapper)
    {
        _roleManager = roleManager;
        _mapper = mapper;
    }

    public Task<IEnumerable<RoleModel>> Handle(GetAllRolesRequest request, CancellationToken cancellationToken)
    {
        var roles = _roleManager.Roles.ToList();
        return Task.FromResult(_mapper.Map<IEnumerable<RoleModel>>(roles));
    }
}
