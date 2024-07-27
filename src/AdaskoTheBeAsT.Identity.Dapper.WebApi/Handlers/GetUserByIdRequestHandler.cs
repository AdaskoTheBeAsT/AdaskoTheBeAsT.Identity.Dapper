using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class GetUserByIdRequestHandler
    : IRequestHandler<GetUserByIdRequest, UserModel?>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public GetUserByIdRequestHandler(UserManager<ApplicationUser> userManager, IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<UserModel?> Handle(GetUserByIdRequest request, CancellationToken cancellationToken)
    {
        if (!request.UserId.HasValue)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(request.UserId!.Value.ToString("D")).ConfigureAwait(false);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var userModel = _mapper.Map<UserModel>(user);
        userModel.Roles = roles;
        return userModel;
    }
}
