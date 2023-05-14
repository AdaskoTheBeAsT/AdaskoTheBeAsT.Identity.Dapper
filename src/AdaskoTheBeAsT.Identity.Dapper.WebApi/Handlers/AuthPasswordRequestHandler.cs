using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class AuthPasswordRequestHandler
    : IRequestHandler<AuthPasswordRequest, Token>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRoleClaimStore<ApplicationUser> _userRoleClaimStore;
    private readonly ITokenService _tokenService;

    public AuthPasswordRequestHandler(
        UserManager<ApplicationUser> userManager,
        IUserRoleClaimStore<ApplicationUser> userRoleClaimStore,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _userRoleClaimStore = userRoleClaimStore;
        _tokenService = tokenService;
    }

    public async Task<Token> Handle(
        AuthPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username ?? string.Empty).ConfigureAwait(false);
        if (user == null)
        {
            throw new UserNotFoundException($"User {request.Username} not found");
        }

        if (!(await _userManager.CheckPasswordAsync(user, request.Password ?? string.Empty).ConfigureAwait(false)))
        {
            throw new InvalidPasswordException();
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var claims = await _userRoleClaimStore.GetUserAndRoleClaimsAsync(user, cancellationToken).ConfigureAwait(false);
        return _tokenService.GenerateToken(user, roles, claims);
    }
}
