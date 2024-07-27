using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class UpdateUserRequestHandler : IRequestHandler<UpdateUserRequest, IdentityResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public UpdateUserRequestHandler(UserManager<ApplicationUser> userManager, IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<IdentityResult> Handle(UpdateUserRequest request, CancellationToken cancellationToken)
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

        _mapper.Map(request, user);
        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return result;
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var rolesToAdd = request.Roles != null ? request.Roles.Except(roles, StringComparer.Ordinal).ToList() : new List<string>();
        var rolesToRemove = roles.Except(request.Roles ?? new List<string>(), StringComparer.Ordinal).ToList();

        var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove).ConfigureAwait(false);
        if (!removeResult.Succeeded)
        {
            return removeResult;
        }

        var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd).ConfigureAwait(false);
        return addResult;
    }
}
