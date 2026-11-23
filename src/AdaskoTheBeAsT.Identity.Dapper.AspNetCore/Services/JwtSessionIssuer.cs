using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Services;

internal sealed class JwtSessionIssuer<TUser>
    where TUser : class
{
    private readonly UserManager<TUser> _userManager;
    private readonly IJwtClaimsProvider<TUser> _claimsProvider;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ITokenSecretHasher _secretHasher;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly ITokenRevocationStore _revocationStore;
    private readonly TimeProvider _timeProvider;
    private readonly IOptionsMonitor<IdentityJwtOptions> _optionsMonitor;

    public JwtSessionIssuer(
        UserManager<TUser> userManager,
        IJwtClaimsProvider<TUser> claimsProvider,
        IJwtTokenGenerator tokenGenerator,
        ITokenSecretHasher secretHasher,
        IRefreshTokenStore refreshTokenStore,
        ITokenRevocationStore revocationStore,
        TimeProvider timeProvider,
        IOptionsMonitor<IdentityJwtOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(claimsProvider);
        ArgumentNullException.ThrowIfNull(tokenGenerator);
        ArgumentNullException.ThrowIfNull(secretHasher);
        ArgumentNullException.ThrowIfNull(refreshTokenStore);
        ArgumentNullException.ThrowIfNull(revocationStore);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _userManager = userManager;
        _claimsProvider = claimsProvider;
        _tokenGenerator = tokenGenerator;
        _secretHasher = secretHasher;
        _refreshTokenStore = refreshTokenStore;
        _revocationStore = revocationStore;
        _timeProvider = timeProvider;
        _optionsMonitor = optionsMonitor;
    }

    public static string? ExtractUserId(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return principal.FindFirstValue(IdentityJwtClaimTypes.Subject)
               ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public static string? ExtractSessionId(ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(IdentityJwtClaimTypes.SessionId)
        ?? principal?.FindFirstValue(ClaimTypes.Sid);

    public static string? ExtractJwtId(ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(IdentityJwtClaimTypes.JwtId);

    public static bool HasMfaAssurance(ClaimsPrincipal? principal) =>
        principal?.Claims.Any(c =>
            (string.Equals(c.Type, IdentityJwtClaimTypes.AuthenticationMethods, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Type, IdentityJwtClaimTypes.MappedAuthenticationMethods, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Type, ClaimTypes.AuthenticationMethod, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(c.Value, "mfa", StringComparison.OrdinalIgnoreCase)) == true;

    public async Task<JwtLoginResponse> IssueNewSessionAsync(
        TUser user,
        IReadOnlyList<string> authenticationMethods,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var options = _optionsMonitor.CurrentValue;
        var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
        var securityStamp = _userManager.SupportsUserSecurityStamp
            ? await _userManager.GetSecurityStampAsync(user).ConfigureAwait(false)
            : null;

        var sessionId = Guid.NewGuid().ToString("N");
        var customClaims = await _claimsProvider.GetClaimsAsync(user, cancellationToken).ConfigureAwait(false);
        var accessToken = _tokenGenerator.GenerateAccessToken(
            new JwtTokenDescriptorContext(userId, sessionId, authenticationMethods, customClaims));

        var sessionExpiresAtUtc = nowUtc + options.AbsoluteSessionLifetime;
        var refreshExpiresAtUtc = nowUtc + options.RefreshTokenLifetime;
        if (refreshExpiresAtUtc > sessionExpiresAtUtc)
        {
            refreshExpiresAtUtc = sessionExpiresAtUtc;
        }

        var rawRefreshToken = _secretHasher.GenerateOpaqueToken();
        var hashedRefresh = _secretHasher.HashToken(rawRefreshToken);

        var refreshRecord = new RefreshTokenRecord(
            Guid.NewGuid(),
            hashedRefresh.HashHex,
            hashedRefresh.KeyId,
            sessionId,
            userId,
            securityStamp,
            string.Join(',', authenticationMethods),
            nowUtc,
            refreshExpiresAtUtc,
            sessionExpiresAtUtc,
            null,
            null,
            null,
            null);

        await _refreshTokenStore.StoreAsync(refreshRecord, cancellationToken).ConfigureAwait(false);

        return new JwtLoginResponse
        {
            TwoFactorRequired = false,
            MfaEnrollmentRequired = false,
            TokenType = "Bearer",
            AccessToken = accessToken.AccessToken,
            ExpiresIn = accessToken.ExpiresInSeconds,
            RefreshToken = rawRefreshToken,
            SessionId = sessionId,
        };
    }

    public async Task RevokeAllUserSessionsAsync(
        string userId,
        string? extraSessionId,
        string? extraJwtId,
        string reason,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var options = _optionsMonitor.CurrentValue;
        var retentionExpiryUtc = nowUtc + options.AccessTokenLifetime + options.ClockSkew;

        var sessionIds = await _refreshTokenStore
            .RevokeAllUserSessionsAsync(userId, nowUtc, reason, cancellationToken)
            .ConfigureAwait(false);

        var uniqueSessions = new HashSet<string>(sessionIds, StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(extraSessionId))
        {
            await _refreshTokenStore
                .RevokeSessionAsync(extraSessionId, nowUtc, reason, cancellationToken)
                .ConfigureAwait(false);
            uniqueSessions.Add(extraSessionId);
        }

        foreach (var sid in uniqueSessions)
        {
            await _revocationStore.RevokeAsync(
                    new TokenRevocationEntry(
                        options.Issuer,
                        RevocationSubjectType.SessionId,
                        sid,
                        nowUtc,
                        retentionExpiryUtc,
                        null,
                        reason),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await RevokeJtiAndUserCutoffAsync(userId, extraJwtId, nowUtc, retentionExpiryUtc, options.Issuer, reason, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task RevokeJtiAndUserCutoffAsync(
        string userId,
        string? extraJwtId,
        DateTimeOffset nowUtc,
        DateTimeOffset retentionExpiryUtc,
        string issuer,
        string reason,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(extraJwtId))
        {
            await _revocationStore.RevokeAsync(
                    new TokenRevocationEntry(
                        issuer,
                        RevocationSubjectType.Jti,
                        extraJwtId,
                        nowUtc,
                        retentionExpiryUtc,
                        null,
                        reason),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var priorSecondCutoffUtc = DateTimeOffset.FromUnixTimeSeconds(nowUtc.ToUnixTimeSeconds() - 1);
        await _revocationStore.RevokeAsync(
                new TokenRevocationEntry(
                    issuer,
                    RevocationSubjectType.UserId,
                    userId,
                    nowUtc,
                    retentionExpiryUtc,
                    priorSecondCutoffUtc,
                    reason),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
