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

public class AuthRefreshTokenRequestHandler
    : IRequestHandler<AuthRefreshTokenRequest, Token>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRoleClaimStore<ApplicationUser> _userRoleClaimStore;
    private readonly ITokenService _tokenService;

    public AuthRefreshTokenRequestHandler(
        UserManager<ApplicationUser> userManager,
        IUserRoleClaimStore<ApplicationUser> userRoleClaimStore,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _userRoleClaimStore = userRoleClaimStore;
        _tokenService = tokenService;
    }

    public async Task<Token> Handle(
        AuthRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var refreshToken = _tokenService.GetRefreshToken(request.RefreshToken ?? string.Empty);
        if (refreshToken == null)
        {
            throw new InvalidRefreshTokenException();
        }

        var user = await _userManager.FindByNameAsync(refreshToken.Subject ?? string.Empty).ConfigureAwait(continueOnCapturedContext: false);
        if (user == null)
        {
            throw new UserNotFoundException($"User {refreshToken.Subject} not found");
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(continueOnCapturedContext: false);
        var claims = await _userRoleClaimStore.GetUserAndRoleClaimsAsync(user, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        return _tokenService.GenerateToken(user, roles, claims);
    }
}
