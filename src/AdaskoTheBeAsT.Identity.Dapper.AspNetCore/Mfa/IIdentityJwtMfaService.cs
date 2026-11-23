using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

#pragma warning disable S2326 // TUser scopes MFA service registrations per Identity user entity
public interface IIdentityJwtMfaService<TUser>
    where TUser : class
{
    Task<JwtServiceResult<JwtMfaStatusResponse>> GetStatusAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<JwtMfaAuthenticatorSetupResponse>> SetupAuthenticatorAsync(
        ClaimsPrincipal? principal,
        JwtMfaAuthenticatorSetupRequest? request,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<JwtMfaAuthenticatorConfirmResponse>> ConfirmAuthenticatorAsync(
        ClaimsPrincipal? principal,
        JwtMfaAuthenticatorConfirmRequest request,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<JwtMfaMethodUpdateResponse>> UpdateMethodAsync(
        ClaimsPrincipal principal,
        JwtMfaMethodUpdateRequest request,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<JwtMfaRecoveryCodesResponse>> RegenerateRecoveryCodesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<bool>> DisableMfaAsync(
        ClaimsPrincipal principal,
        JwtMfaDisableRequest request,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<JwtMfaSetupLinkResponse>> SendSetupLinkAsync(
        ClaimsPrincipal principal,
        JwtMfaSetupLinkRequest? request,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<JwtMfaSetupLinkExchangeResponse>> ExchangeSetupLinkAsync(
        JwtMfaSetupLinkExchangeRequest request,
        CancellationToken cancellationToken);
}
#pragma warning restore S2326
