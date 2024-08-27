using System;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class CreateUserRequestHandler
    : IRequestHandler<CreateUserRequest, IdentityResult>
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateUserRequestHandler(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IdentityResult> Handle(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var roleName in request.Roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName).ConfigureAwait(continueOnCapturedContext: false);
                if (role == null)
                {
                    role = new ApplicationRole
                    {
                        ConcurrencyStamp = Guid.NewGuid().ToString("D"),
                        Name = roleName,
                    };

                    var roleResult = await _roleManager.CreateAsync(role).ConfigureAwait(continueOnCapturedContext: false);
                    if (!roleResult.Succeeded)
                    {
                        return roleResult;
                    }
                }
            }

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.UserName,
                ConcurrencyStamp = Guid.NewGuid().ToString("D"),
                LockoutEnabled = false,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("D"),
            };

            var result = await _userManager.CreateAsync(
                    user,
                    request.Password ?? string.Empty)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (!result.Succeeded)
            {
                return result;
            }

            var userRoleResult = await _userManager.AddToRolesAsync(user, request.Roles).ConfigureAwait(continueOnCapturedContext: false);
            return userRoleResult;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "1",
                Description = ex.Message,
            });
        }
    }
}
