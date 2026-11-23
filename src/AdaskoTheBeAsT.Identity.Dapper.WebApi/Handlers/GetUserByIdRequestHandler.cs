using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class GetUserByIdRequestHandler
    : IRequestHandler<GetUserByIdRequest, UserModel?>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebApiMapper _mapper;

    public GetUserByIdRequestHandler(
        UserManager<ApplicationUser> userManager,
        IWebApiMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<UserModel?> Handle(
        GetUserByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.UserId.HasValue)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(request.UserId.Value.ToString("D")).ConfigureAwait(continueOnCapturedContext: false);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(continueOnCapturedContext: false);
        return _mapper.MapToUserModel(user, roles);
    }
}
