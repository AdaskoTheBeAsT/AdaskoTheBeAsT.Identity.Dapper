using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class CreateRoleRequestHandler
    : IRequestHandler<CreateRoleRequest, IdentityResult>
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public CreateRoleRequestHandler(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<IdentityResult> Handle(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = new ApplicationRole { Name = request.Name };
        var result = await _roleManager.CreateAsync(role).ConfigureAwait(false);
        return result;
    }
}
