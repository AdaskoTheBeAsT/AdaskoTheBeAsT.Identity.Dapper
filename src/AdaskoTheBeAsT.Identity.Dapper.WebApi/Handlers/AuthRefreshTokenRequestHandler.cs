using System;
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
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthRefreshTokenRequestHandler(
        UserManager<ApplicationUser> userManager,
        IUserRoleClaimStore<ApplicationUser> userRoleClaimStore,
        ITokenService tokenService,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _userRoleClaimStore = userRoleClaimStore;
        _tokenService = tokenService;
        _signInManager = signInManager;
    }

    public async Task<Token> Handle(
        AuthRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var refreshToken = _tokenService.ConsumeRefreshToken(request.RefreshToken ?? string.Empty);
        var user = await _userManager.FindByIdAsync(refreshToken.Subject ?? string.Empty).ConfigureAwait(continueOnCapturedContext: false);
        if (user == null ||
            !string.Equals(user.SecurityStamp, refreshToken.SecurityStamp, StringComparison.Ordinal) ||
            !await _signInManager.CanSignInAsync(user).ConfigureAwait(continueOnCapturedContext: false) ||
            (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user).ConfigureAwait(continueOnCapturedContext: false)))
        {
            throw new InvalidRefreshTokenException();
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(continueOnCapturedContext: false);
        var claims = await _userRoleClaimStore.GetUserAndRoleClaimsAsync(user, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        return _tokenService.GenerateToken(user, roles, claims);
    }
}
