using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class DeleteUserRequestHandler
    : IRequestHandler<DeleteUserRequest, IdentityResult>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DeleteUserRequestHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IdentityResult> Handle(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        if (!request.UserId.HasValue)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User id not provided" });
        }

        var user = await _userManager.FindByIdAsync(request.UserId.Value.ToString("D")).ConfigureAwait(false);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        var result = await _userManager.DeleteAsync(user).ConfigureAwait(false);
        return result;
    }
}
