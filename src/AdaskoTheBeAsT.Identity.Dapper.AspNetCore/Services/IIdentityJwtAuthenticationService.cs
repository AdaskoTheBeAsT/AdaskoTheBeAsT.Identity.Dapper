using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Services;

#pragma warning disable S2326 // TUser scopes authentication service registrations per Identity user entity
public interface IIdentityJwtAuthenticationService<TUser>
    where TUser : class
{
    Task<JwtServiceResult<JwtLoginResponse>> LoginAsync(
        JwtLoginRequest request,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<JwtLoginResponse>> VerifyMfaAsync(
        JwtMfaVerifyRequest request,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<JwtMfaResendResponse>> ResendMfaCodeAsync(
        JwtMfaResendRequest request,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<JwtLoginResponse>> RefreshAsync(
        JwtRefreshRequest request,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<bool>> RevokeAsync(
        ClaimsPrincipal principal,
        JwtRevokeRequest? request,
        CancellationToken cancellationToken);

    Task<JwtServiceResult<JwtRegisterResponse>> RegisterAsync(
        JwtRegisterRequest request,
        CancellationToken cancellationToken);
}
#pragma warning restore S2326
