using System;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Services;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class AuthPasswordRequestHandler
    : IRequestHandler<AuthPasswordRequest, Token>
{
    private readonly IIdentityJwtAuthenticationService<ApplicationUser> _authenticationService;

    public AuthPasswordRequestHandler(
        IIdentityJwtAuthenticationService<ApplicationUser> authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        _authenticationService = authenticationService;
    }

    public async Task<Token> Handle(
        AuthPasswordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _authenticationService
            .LoginAsync(
                new JwtLoginRequest
                {
                    UserName = request.Username ?? string.Empty,
                    Password = request.Password ?? string.Empty,
                },
                cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        if (result.Status == JwtOperationStatus.InvalidRequest)
        {
            throw new RequestValidationException(result.ErrorDescription);
        }

        // The legacy /api/token form endpoint has no second-factor step; never bypass MFA.
        if (!result.Succeeded ||
            result.Value == null ||
            result.Value.TwoFactorRequired ||
            string.IsNullOrWhiteSpace(result.Value.AccessToken))
        {
            throw new InvalidPasswordException();
        }

        return LegacyTokenMapper.Map(result.Value);
    }
}
