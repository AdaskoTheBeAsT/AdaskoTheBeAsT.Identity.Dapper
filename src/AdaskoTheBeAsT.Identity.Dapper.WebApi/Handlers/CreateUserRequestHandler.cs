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
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateUserRequestHandler(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IdentityResult> Handle(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Roles is { Count: > 0 })
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "RoleAssignmentNotAllowed",
                Description = "Roles cannot be assigned during public registration.",
            });
        }

        try
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.UserName,
                ConcurrencyStamp = Guid.NewGuid().ToString("D"),
                LockoutEnabled = true,
                EmailConfirmed = false,
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

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = IdentityErrorCodes.CreateFailed,
                Description = "User creation failed.",
            });
        }
    }
}
