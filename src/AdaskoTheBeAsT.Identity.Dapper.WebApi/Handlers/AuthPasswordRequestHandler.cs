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
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthPasswordRequestHandler(
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
        AuthPasswordRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByNameAsync(request.Username ?? string.Empty).ConfigureAwait(continueOnCapturedContext: false);
        if (user == null)
        {
            throw new InvalidPasswordException();
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password ?? string.Empty, lockoutOnFailure: true)
            .ConfigureAwait(continueOnCapturedContext: false);

        // This example has no second-factor exchange endpoint; never bypass one.
        if (!result.Succeeded ||
            (_userManager.SupportsUserTwoFactor && await _userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(continueOnCapturedContext: false)))
        {
            throw new InvalidPasswordException();
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(continueOnCapturedContext: false);
        var claims = await _userRoleClaimStore.GetUserAndRoleClaimsAsync(user, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        return _tokenService.GenerateToken(user, roles, claims);
    }
}
