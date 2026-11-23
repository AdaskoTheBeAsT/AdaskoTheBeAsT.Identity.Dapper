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

public class AuthRefreshTokenRequestHandler
    : IRequestHandler<AuthRefreshTokenRequest, Token>
{
    private readonly IIdentityJwtAuthenticationService<ApplicationUser> _authenticationService;

    public AuthRefreshTokenRequestHandler(
        IIdentityJwtAuthenticationService<ApplicationUser> authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        _authenticationService = authenticationService;
    }

    public async Task<Token> Handle(
        AuthRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _authenticationService
            .RefreshAsync(
                new JwtRefreshRequest
                {
                    RefreshToken = request.RefreshToken ?? string.Empty,
                },
                cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        if (!result.Succeeded ||
            result.Value == null ||
            string.IsNullOrWhiteSpace(result.Value.AccessToken))
        {
            throw new InvalidRefreshTokenException();
        }

        return LegacyTokenMapper.Map(result.Value);
    }
}
