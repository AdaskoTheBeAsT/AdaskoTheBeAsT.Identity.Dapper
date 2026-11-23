using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;
using AdaskoTheBeAsT.Identity.Dapper.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Services;

internal sealed class IdentityJwtAuthenticationService<TUser> : IIdentityJwtAuthenticationService<TUser>
    where TUser : class
{
    private const int MaxCredentialLength = 256;

    private readonly UserManager<TUser> _userManager;
    private readonly SignInManager<TUser> _signInManager;
    private readonly IJwtClaimsProvider<TUser> _claimsProvider;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ITokenSecretHasher _secretHasher;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly ITokenRevocationStore _revocationStore;
    private readonly IMfaChallengeStore _challengeStore;
    private readonly IMfaRequirementPolicy<TUser> _mfaPolicy;
    private readonly IMfaCodeSender<TUser> _mfaCodeSender;
    private readonly MfaUserTokenManager<TUser> _mfaTokenManager;
    private readonly JwtSessionIssuer<TUser> _sessionIssuer;
    private readonly TimeProvider _timeProvider;
    private readonly IOptionsMonitor<IdentityJwtOptions> _optionsMonitor;

    public IdentityJwtAuthenticationService(
        UserManager<TUser> userManager,
        SignInManager<TUser> signInManager,
        IJwtClaimsProvider<TUser> claimsProvider,
        IJwtTokenGenerator tokenGenerator,
        ITokenSecretHasher secretHasher,
        IRefreshTokenStore refreshTokenStore,
        ITokenRevocationStore revocationStore,
        IMfaChallengeStore challengeStore,
        IMfaRequirementPolicy<TUser> mfaPolicy,
        IMfaCodeSender<TUser> mfaCodeSender,
        MfaUserTokenManager<TUser> mfaTokenManager,
        JwtSessionIssuer<TUser> sessionIssuer,
        TimeProvider timeProvider,
        IOptionsMonitor<IdentityJwtOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(claimsProvider);
        ArgumentNullException.ThrowIfNull(tokenGenerator);
        ArgumentNullException.ThrowIfNull(secretHasher);
        ArgumentNullException.ThrowIfNull(refreshTokenStore);
        ArgumentNullException.ThrowIfNull(revocationStore);
        ArgumentNullException.ThrowIfNull(challengeStore);
        ArgumentNullException.ThrowIfNull(mfaPolicy);
        ArgumentNullException.ThrowIfNull(mfaCodeSender);
        ArgumentNullException.ThrowIfNull(mfaTokenManager);
        ArgumentNullException.ThrowIfNull(sessionIssuer);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _userManager = userManager;
        _signInManager = signInManager;
        _claimsProvider = claimsProvider;
        _tokenGenerator = tokenGenerator;
        _secretHasher = secretHasher;
        _refreshTokenStore = refreshTokenStore;
        _revocationStore = revocationStore;
        _challengeStore = challengeStore;
        _mfaPolicy = mfaPolicy;
        _mfaCodeSender = mfaCodeSender;
        _mfaTokenManager = mfaTokenManager;
        _sessionIssuer = sessionIssuer;
        _timeProvider = timeProvider;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<JwtServiceResult<JwtLoginResponse>> LoginAsync(
        JwtLoginRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var identifier = !string.IsNullOrWhiteSpace(request.UserName) ? request.UserName : request.Email;
        if (!IsValidCredentialInput(identifier) || !IsValidCredentialInput(request.Password))
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_request",
                "Username and password must be between 1 and 256 characters.");
        }

        var user = await FindUserByIdentifierAsync(identifier!).ConfigureAwait(false);
        if (user == null)
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_grant",
                "Invalid credentials.");
        }

        var signInResult = await _signInManager
            .CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)
            .ConfigureAwait(false);

        if (!signInResult.Succeeded)
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_grant",
                "Invalid credentials.");
        }

        if (!await _mfaPolicy.IsMfaRequiredAsync(user, cancellationToken).ConfigureAwait(false))
        {
            var tokenResponse = await _sessionIssuer
                .IssueNewSessionAsync(user, ["pwd"], cancellationToken)
                .ConfigureAwait(false);
            return JwtServiceResult<JwtLoginResponse>.Success(tokenResponse);
        }

        return await CreateMfaChallengeResponseAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JwtServiceResult<JwtLoginResponse>> VerifyMfaAsync(
        JwtMfaVerifyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.MfaChallengeToken) ||
            string.IsNullOrWhiteSpace(request.Method) ||
            string.IsNullOrWhiteSpace(request.Code))
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_request",
                "Challenge token, method, and verification code are required.");
        }

        if (!Enum.TryParse<MfaMethodType>(request.Method, ignoreCase: true, out var method))
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_method",
                "Unsupported MFA method.");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var (record, matchedDigest, lookupError) = await LoadAndIncrementLoginChallengeAsync(
                request.MfaChallengeToken,
                nowUtc,
                cancellationToken)
            .ConfigureAwait(false);

        if (lookupError != null)
        {
            return lookupError;
        }

        return await CompleteMfaVerificationAsync(record!, matchedDigest!, method, request.Code, nowUtc, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<JwtServiceResult<JwtMfaResendResponse>> ResendMfaCodeAsync(
        JwtMfaResendRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.MfaChallengeToken) ||
            !Enum.TryParse<MfaMethodType>(request.Method, ignoreCase: true, out var method) ||
            method is not (MfaMethodType.Email or MfaMethodType.Sms))
        {
            return JwtServiceResult<JwtMfaResendResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_request",
                "Valid challenge token and Email or Sms method are required.");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var (record, matchedDigest) = await FindLoginChallengeAsync(request.MfaChallengeToken, nowUtc, cancellationToken)
            .ConfigureAwait(false);

        if (record == null || matchedDigest == null)
        {
            return JwtServiceResult<JwtMfaResendResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_mfa_challenge",
                "MFA challenge is invalid or expired.");
        }

        return await ExecuteResendAsync(record, matchedDigest, method, nowUtc, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JwtServiceResult<JwtLoginResponse>> RefreshAsync(
        JwtRefreshRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_request",
                "Refresh token is required.");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var options = _optionsMonitor.CurrentValue;
        var (existing, matchedDigest) = await FindRefreshTokenAsync(request.RefreshToken, cancellationToken)
            .ConfigureAwait(false);

        if (existing == null || matchedDigest == null)
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_grant",
                "Invalid refresh token.");
        }

        if (existing.ConsumedAtUtc.HasValue)
        {
            await HandleReplayCascadeAsync(existing.SessionId, nowUtc, options, cancellationToken).ConfigureAwait(false);
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_grant",
                "Refresh token replay detected; session revoked.");
        }

        if (existing.RevokedAtUtc.HasValue || existing.ExpiresAtUtc <= nowUtc || existing.SessionExpiresAtUtc <= nowUtc)
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_grant",
                "Refresh token is expired or revoked.");
        }

        return await ValidateAndRotateRefreshSessionAsync(existing, matchedDigest, nowUtc, options, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<JwtServiceResult<bool>> RevokeAsync(
        ClaimsPrincipal principal,
        JwtRevokeRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = JwtSessionIssuer<TUser>.ExtractUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return JwtServiceResult<bool>.Fail(JwtOperationStatus.Unauthorized, "unauthorized");
        }

        var scope = RevokeScope.Session;
        if (!string.IsNullOrWhiteSpace(request?.Scope) &&
            !Enum.TryParse(request.Scope, ignoreCase: true, out scope))
        {
            return JwtServiceResult<bool>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_scope",
                "Scope must be Token, Session, or AllSessions.");
        }

        return await ExecuteRevokeScopeAsync(principal, userId, scope, request?.UpdateSecurityStamp == true, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<JwtServiceResult<JwtRegisterResponse>> RegisterAsync(
        JwtRegisterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var userName = string.IsNullOrWhiteSpace(request.UserName) ? request.Email : request.UserName;
        if (!IsValidCredentialInput(request.Email) ||
            !IsValidCredentialInput(request.Password) ||
            !IsValidCredentialInput(userName))
        {
            return JwtServiceResult<JwtRegisterResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_request",
                "Email, username, and password must be between 1 and 256 characters.");
        }

        var user = Activator.CreateInstance<TUser>();
        InitializeInsertOwnIdIfNeeded(user);
        await _userManager.SetUserNameAsync(user, userName).ConfigureAwait(false);
        if (_userManager.SupportsUserEmail)
        {
            await _userManager.SetEmailAsync(user, request.Email).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && _userManager.SupportsUserPhoneNumber)
        {
            await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber).ConfigureAwait(false);
        }

        var result = await _userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault();
            var status = firstError?.Code?.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) == true
                ? JwtOperationStatus.Conflict
                : JwtOperationStatus.InvalidRequest;
            return JwtServiceResult<JwtRegisterResponse>.Fail(
                status,
                firstError?.Code ?? "registration_failed",
                firstError?.Description ?? "Could not create user.");
        }

        var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
        return JwtServiceResult<JwtRegisterResponse>.Success(new JwtRegisterResponse(userId));
    }

    private static bool IsValidCredentialInput(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length is >= 1 and <= MaxCredentialLength;

    private static void InitializeInsertOwnIdIfNeeded(TUser user)
    {
        if (!Attribute.IsDefined(typeof(TUser), typeof(InsertOwnIdAttribute)))
        {
            return;
        }

        if (user is IdentityUser<Guid> guidUser && guidUser.Id == Guid.Empty)
        {
            guidUser.Id = Guid.CreateVersion7();
        }
        else if (user is IdentityUser<string> stringUser && string.IsNullOrWhiteSpace(stringUser.Id))
        {
            stringUser.Id = Guid.CreateVersion7().ToString();
        }
    }

    private static bool IsMethodAllowedOnChallenge(
        string allowedMethodsCsv,
        MfaMethodType method)
    {
        var allowed = allowedMethodsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return allowed.Any(a => string.Equals(a, method.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private static DateTimeOffset ExtractExpirationOrFallback(
        ClaimsPrincipal principal,
        DateTimeOffset fallback)
    {
        var rawExp = principal.FindFirstValue(IdentityJwtClaimTypes.Expiration);
        return long.TryParse(rawExp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixExp)
            ? DateTimeOffset.FromUnixTimeSeconds(unixExp)
            : fallback;
    }

    private static JwtServiceResult<JwtMfaResendResponse>? GetResendLimit(
        MfaChallengeRecord record,
        DateTimeOffset nowUtc,
        IdentityJwtOptions options)
    {
        if (record.ResendAvailableAtUtc.HasValue && record.ResendAvailableAtUtc.Value > nowUtc)
        {
            var remaining = (int)Math.Max(1, Math.Ceiling((record.ResendAvailableAtUtc.Value - nowUtc).TotalSeconds));
            return new JwtServiceResult<JwtMfaResendResponse>(
                JwtOperationStatus.TooManyRequests,
                new JwtMfaResendResponse(Sent: false, remaining),
                "resend_cooldown_active",
                "Please wait before requesting another code.");
        }

        if (record.ResendCount >= options.Mfa.MaxResendCount)
        {
            return JwtServiceResult<JwtMfaResendResponse>.Fail(
                JwtOperationStatus.TooManyRequests,
                "max_resends_exceeded",
                "Maximum number of code resends reached for this challenge.");
        }

        return null;
    }

    private async Task<TUser?> FindUserByIdentifierAsync(string identifier)
    {
        var user = await _userManager.FindByNameAsync(identifier).ConfigureAwait(false);
        if (user == null && _userManager.SupportsUserEmail && identifier.Contains('@', StringComparison.Ordinal))
        {
            user = await _userManager.FindByEmailAsync(identifier).ConfigureAwait(false);
        }

        return user;
    }

    private async Task<(MfaChallengeRecord? Record, string? Digest, JwtServiceResult<JwtLoginResponse>? Error)> LoadAndIncrementLoginChallengeAsync(
        string rawChallengeToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        foreach (var hashHex in _secretHasher.ComputeCandidateTokenHashes(rawChallengeToken).Select(candidate => candidate.HashHex))
        {
            var attempt = await _challengeStore
                .IncrementAttemptAsync(hashHex, nowUtc, cancellationToken)
                .ConfigureAwait(false);

            if (attempt.Status == MfaChallengeAttemptStatus.NotFoundOrExpired)
            {
                continue;
            }

            if (attempt.Status == MfaChallengeAttemptStatus.Exhausted)
            {
                await RecordExhaustedLockoutFailureAsync(attempt.Record?.UserId).ConfigureAwait(false);
                return (
                    null,
                    null,
                    JwtServiceResult<JwtLoginResponse>.Fail(
                        JwtOperationStatus.Unauthorized,
                        "mfa_challenge_exhausted",
                        "Maximum verification attempts exceeded."));
            }

            if (attempt.Record is { Purpose: MfaChallengePurpose.LoginChallenge })
            {
                return (attempt.Record, hashHex, null);
            }
        }

        return (
            null,
            null,
            JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_mfa_challenge",
                "MFA challenge is invalid or expired."));
    }

    private async Task RecordExhaustedLockoutFailureAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || !_userManager.SupportsUserLockout)
        {
            return;
        }

        var exhaustedUser = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (exhaustedUser != null)
        {
            await _userManager.AccessFailedAsync(exhaustedUser).ConfigureAwait(false);
        }
    }

    private async Task<JwtServiceResult<JwtLoginResponse>> CompleteMfaVerificationAsync(
        MfaChallengeRecord record,
        string matchedDigest,
        MfaMethodType method,
        string rawCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (!IsMethodAllowedOnChallenge(record.AllowedMethods, method))
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "method_not_allowed",
                "Requested MFA method is not available for this challenge.");
        }

        var user = await _userManager.FindByIdAsync(record.UserId).ConfigureAwait(false);
        if (user == null || !await IsAccountEligibleAsync(user, record.SecurityStamp).ConfigureAwait(false))
        {
            await _challengeStore.ConsumeAsync(matchedDigest, nowUtc, cancellationToken).ConfigureAwait(false);
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_grant",
                "User is no longer eligible to sign in.");
        }

        var (verified, methodAmr) = await VerifySecondFactorCodeAsync(user, record, method, rawCode, nowUtc, cancellationToken)
            .ConfigureAwait(false);

        if (!verified)
        {
            if (record.AttemptCount >= record.MaxAttempts)
            {
                await _challengeStore.ConsumeAsync(matchedDigest, nowUtc, cancellationToken).ConfigureAwait(false);
                if (_userManager.SupportsUserLockout)
                {
                    await _userManager.AccessFailedAsync(user).ConfigureAwait(false);
                }
            }

            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_mfa_code",
                "Invalid verification code.");
        }

        if (!await _challengeStore.ConsumeAsync(matchedDigest, nowUtc, cancellationToken).ConfigureAwait(false))
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_mfa_challenge",
                "MFA challenge was already consumed.");
        }

        if (_userManager.SupportsUserLockout)
        {
            await _userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        }

        var session = await _sessionIssuer
            .IssueNewSessionAsync(user, ["pwd", "mfa", methodAmr], cancellationToken)
            .ConfigureAwait(false);

        return JwtServiceResult<JwtLoginResponse>.Success(session);
    }

    private async Task<(MfaChallengeRecord? Record, string? Digest)> FindLoginChallengeAsync(
        string rawChallengeToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        foreach (var hashHex in _secretHasher.ComputeCandidateTokenHashes(rawChallengeToken).Select(candidate => candidate.HashHex))
        {
            var found = await _challengeStore.FindAsync(hashHex, nowUtc, cancellationToken).ConfigureAwait(false);
            if (found is { Purpose: MfaChallengePurpose.LoginChallenge })
            {
                return (found, hashHex);
            }
        }

        return (null, null);
    }

    private async Task<JwtServiceResult<JwtMfaResendResponse>> ExecuteResendAsync(
        MfaChallengeRecord record,
        string matchedDigest,
        MfaMethodType method,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (!IsMethodAllowedOnChallenge(record.AllowedMethods, method))
        {
            return JwtServiceResult<JwtMfaResendResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "method_not_allowed",
                "Requested MFA method is not available for this challenge.");
        }

        var options = _optionsMonitor.CurrentValue;
        var rateLimited = GetResendLimit(record, nowUtc, options);
        if (rateLimited != null)
        {
            return rateLimited;
        }

        var user = await _userManager.FindByIdAsync(record.UserId).ConfigureAwait(false);
        if (user == null || !await IsAccountEligibleAsync(user, record.SecurityStamp).ConfigureAwait(false))
        {
            return JwtServiceResult<JwtMfaResendResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_mfa_challenge",
                "User is no longer eligible.");
        }

        return await RotateAndSendChallengeOtpAsync(user, record, matchedDigest, method, nowUtc, options, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<JwtServiceResult<JwtMfaResendResponse>> RotateAndSendChallengeOtpAsync(
        TUser user,
        MfaChallengeRecord record,
        string matchedDigest,
        MfaMethodType method,
        DateTimeOffset nowUtc,
        IdentityJwtOptions options,
        CancellationToken cancellationToken)
    {
        var rawOtp = _secretHasher.GenerateSixDigitCode();
        var otpScope = string.Create(CultureInfo.InvariantCulture, $"login:{record.ChallengeId}:{method}");
        var hashedOtp = _secretHasher.HashScopedCode(otpScope, rawOtp);
        var nextResendAt = nowUtc + options.Mfa.ResendCooldown;

        var updated = await _challengeStore.UpdateOtpStateAsync(
                matchedDigest,
                method.ToString(),
                hashedOtp.HashHex,
                hashedOtp.KeyId,
                record.ResendCount + 1,
                nextResendAt,
                nowUtc,
                cancellationToken)
            .ConfigureAwait(false);

        if (!updated)
        {
            var current = await _challengeStore.FindAsync(matchedDigest, nowUtc, cancellationToken).ConfigureAwait(false);
            if (current != null && GetResendLimit(current, nowUtc, options) is { } rateLimited)
            {
                return rateLimited;
            }

            return JwtServiceResult<JwtMfaResendResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_mfa_challenge",
                "MFA challenge expired.");
        }

        await DispatchChallengeCodeAsync(user, method, rawOtp, cancellationToken).ConfigureAwait(false);
        var retryAfter = (int)Math.Max(1, Math.Ceiling(options.Mfa.ResendCooldown.TotalSeconds));
        return JwtServiceResult<JwtMfaResendResponse>.Success(new JwtMfaResendResponse(Sent: true, retryAfter));
    }

    private async Task<(RefreshTokenRecord? Record, string? Digest)> FindRefreshTokenAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken)
    {
        foreach (var hashHex in _secretHasher.ComputeCandidateTokenHashes(rawRefreshToken).Select(candidate => candidate.HashHex))
        {
            var found = await _refreshTokenStore.FindByHashAsync(hashHex, cancellationToken).ConfigureAwait(false);
            if (found != null)
            {
                return (found, hashHex);
            }
        }

        return (null, null);
    }

    private async Task<JwtServiceResult<JwtLoginResponse>> ValidateAndRotateRefreshSessionAsync(
        RefreshTokenRecord existing,
        string matchedDigest,
        DateTimeOffset nowUtc,
        IdentityJwtOptions options,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(existing.UserId).ConfigureAwait(false);
        if (user == null || !await IsAccountEligibleAsync(user, existing.SecurityStamp).ConfigureAwait(false))
        {
            await _refreshTokenStore
                .RevokeSessionAsync(existing.SessionId, nowUtc, "account_ineligible", cancellationToken)
                .ConfigureAwait(false);
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_grant",
                "Account state invalidated refresh session.");
        }

        var amrList = existing.AuthenticationMethods
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (amrList.Length == 0)
        {
            amrList = ["pwd"];
        }

        if (await _mfaPolicy.IsMfaRequiredAsync(user, cancellationToken).ConfigureAwait(false) &&
            !amrList.Contains("mfa", StringComparer.OrdinalIgnoreCase))
        {
            await _refreshTokenStore
                .RevokeSessionAsync(existing.SessionId, nowUtc, "mfa_step_up_required", cancellationToken)
                .ConfigureAwait(false);
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "mfa_required",
                "Multi-factor authentication is now required.");
        }

        return await PerformAtomicRefreshRotationAsync(user, existing, matchedDigest, amrList, nowUtc, options, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<JwtServiceResult<JwtLoginResponse>> PerformAtomicRefreshRotationAsync(
        TUser user,
        RefreshTokenRecord existing,
        string matchedDigest,
        string[] amrList,
        DateTimeOffset nowUtc,
        IdentityJwtOptions options,
        CancellationToken cancellationToken)
    {
        var (newRawRefreshToken, currentUserId, replacement) = await BuildReplacementRefreshTokenAsync(
                user,
                existing,
                amrList,
                nowUtc,
                options)
            .ConfigureAwait(false);

        var rotation = await _refreshTokenStore
            .RotateAsync(matchedDigest, replacement, nowUtc, cancellationToken)
            .ConfigureAwait(false);

        if (rotation.Status == RefreshTokenRotationStatus.Replayed)
        {
            await HandleReplayCascadeAsync(existing.SessionId, nowUtc, options, cancellationToken).ConfigureAwait(false);
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_grant",
                "Refresh token replay detected; session revoked.");
        }

        if (rotation.Status != RefreshTokenRotationStatus.Rotated)
        {
            return JwtServiceResult<JwtLoginResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_grant",
                "Refresh token could not be rotated.");
        }

        var customClaims = await _claimsProvider.GetClaimsAsync(user, cancellationToken).ConfigureAwait(false);
        var accessToken = _tokenGenerator.GenerateAccessToken(
            new JwtTokenDescriptorContext(currentUserId, existing.SessionId, amrList, customClaims));

        return JwtServiceResult<JwtLoginResponse>.Success(
            new JwtLoginResponse
            {
                TokenType = "Bearer",
                AccessToken = accessToken.AccessToken,
                ExpiresIn = accessToken.ExpiresInSeconds,
                RefreshToken = newRawRefreshToken,
                SessionId = existing.SessionId,
            });
    }

    private async Task<(string RawRefreshToken, string UserId, RefreshTokenRecord Replacement)> BuildReplacementRefreshTokenAsync(
        TUser user,
        RefreshTokenRecord existing,
        string[] amrList,
        DateTimeOffset nowUtc,
        IdentityJwtOptions options)
    {
        var newRawRefreshToken = _secretHasher.GenerateOpaqueToken();
        var newHashed = _secretHasher.HashToken(newRawRefreshToken);
        var nextRefreshExpiry = nowUtc + options.RefreshTokenLifetime;
        if (nextRefreshExpiry > existing.SessionExpiresAtUtc)
        {
            nextRefreshExpiry = existing.SessionExpiresAtUtc;
        }

        var currentUserId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
        var currentStamp = _userManager.SupportsUserSecurityStamp
            ? await _userManager.GetSecurityStampAsync(user).ConfigureAwait(false)
            : null;

        var replacement = new RefreshTokenRecord(
            Guid.NewGuid(),
            newHashed.HashHex,
            newHashed.KeyId,
            existing.SessionId,
            currentUserId,
            currentStamp,
            string.Join(',', amrList),
            nowUtc,
            nextRefreshExpiry,
            existing.SessionExpiresAtUtc,
            null,
            null,
            null,
            null);

        return (newRawRefreshToken, currentUserId, replacement);
    }

    private async Task<JwtServiceResult<bool>> ExecuteRevokeScopeAsync(
        ClaimsPrincipal principal,
        string userId,
        RevokeScope scope,
        bool requestStampUpdate,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var options = _optionsMonitor.CurrentValue;
        var sessionId = JwtSessionIssuer<TUser>.ExtractSessionId(principal);
        var jti = JwtSessionIssuer<TUser>.ExtractJwtId(principal);
        var tokenExpUtc = ExtractExpirationOrFallback(principal, nowUtc + options.AccessTokenLifetime);
        var retentionExpiryUtc = tokenExpUtc + options.ClockSkew;

        return scope switch
        {
            RevokeScope.Token => await RevokeSingleTokenAsync(jti, nowUtc, retentionExpiryUtc, options.Issuer, cancellationToken)
                .ConfigureAwait(false),
            RevokeScope.Session => await RevokeCurrentSessionAsync(sessionId, jti, nowUtc, retentionExpiryUtc, options, cancellationToken)
                .ConfigureAwait(false),
            RevokeScope.AllSessions => await RevokeAllSessionsForUserAsync(
                    userId,
                    sessionId,
                    jti,
                    requestStampUpdate || options.UpdateSecurityStampOnRevokeAll,
                    cancellationToken)
                .ConfigureAwait(false),
            _ => JwtServiceResult<bool>.Fail(JwtOperationStatus.InvalidRequest, "invalid_scope"),
        };
    }

    private async Task<JwtServiceResult<bool>> RevokeSingleTokenAsync(
        string? jti,
        DateTimeOffset nowUtc,
        DateTimeOffset retentionExpiryUtc,
        string issuer,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return JwtServiceResult<bool>.Fail(
                JwtOperationStatus.InvalidRequest,
                "missing_jti",
                "Current access token does not contain a jti claim.");
        }

        await _revocationStore.RevokeAsync(
                new TokenRevocationEntry(issuer, RevocationSubjectType.Jti, jti, nowUtc, retentionExpiryUtc, null, "RevokeToken"),
                cancellationToken)
            .ConfigureAwait(false);
        return JwtServiceResult<bool>.Success(true);
    }

    private async Task<JwtServiceResult<bool>> RevokeCurrentSessionAsync(
        string? sessionId,
        string? jti,
        DateTimeOffset nowUtc,
        DateTimeOffset tokenRetentionUtc,
        IdentityJwtOptions options,
        CancellationToken cancellationToken)
    {
        var sessionRetentionUtc = nowUtc + options.AccessTokenLifetime + options.ClockSkew;
        if (tokenRetentionUtc > sessionRetentionUtc)
        {
            sessionRetentionUtc = tokenRetentionUtc;
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _refreshTokenStore.RevokeSessionAsync(sessionId, nowUtc, "Logout", cancellationToken).ConfigureAwait(false);
            await _revocationStore.RevokeAsync(
                    new TokenRevocationEntry(options.Issuer, RevocationSubjectType.SessionId, sessionId, nowUtc, sessionRetentionUtc, null, "Logout"),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(jti))
        {
            await _revocationStore.RevokeAsync(
                    new TokenRevocationEntry(options.Issuer, RevocationSubjectType.Jti, jti, nowUtc, sessionRetentionUtc, null, "Logout"),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return JwtServiceResult<bool>.Success(true);
    }

    private async Task<JwtServiceResult<bool>> RevokeAllSessionsForUserAsync(
        string userId,
        string? sessionId,
        string? jti,
        bool updateSecurityStamp,
        CancellationToken cancellationToken)
    {
        await _sessionIssuer.RevokeAllUserSessionsAsync(userId, sessionId, jti, "RevokeAllSessions", cancellationToken)
            .ConfigureAwait(false);

        if (updateSecurityStamp && _userManager.SupportsUserSecurityStamp)
        {
            var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user != null)
            {
                await _userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);
            }
        }

        return JwtServiceResult<bool>.Success(true);
    }

    private async Task<JwtServiceResult<JwtLoginResponse>> CreateMfaChallengeResponseAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var nowUtc = _timeProvider.GetUtcNow();
        var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
        var securityStamp = _userManager.SupportsUserSecurityStamp
            ? await _userManager.GetSecurityStampAsync(user).ConfigureAwait(false)
            : null;

        var enabledMethods = (await _mfaTokenManager.GetEnabledMethodsAsync(user).ConfigureAwait(false))
            .Where(m =>
                (m != MfaMethodType.Email || options.Mfa.AllowEmailMfa) &&
                (m != MfaMethodType.Sms || options.Mfa.AllowSmsMfa))
            .ToList();

        return enabledMethods.Count == 0
            ? await CreateBootstrapEnrollmentChallengeAsync(userId, securityStamp, nowUtc, options, cancellationToken)
                .ConfigureAwait(false)
            : await CreateEnrolledLoginChallengeAsync(user, userId, securityStamp, enabledMethods, nowUtc, options, cancellationToken)
                .ConfigureAwait(false);
    }

    private async Task<JwtServiceResult<JwtLoginResponse>> CreateBootstrapEnrollmentChallengeAsync(
        string userId,
        string? securityStamp,
        DateTimeOffset nowUtc,
        IdentityJwtOptions options,
        CancellationToken cancellationToken)
    {
        var challengeExpiresIn = (int)Math.Max(1, Math.Ceiling(options.Mfa.ChallengeLifetime.TotalSeconds));
        var rawChallengeToken = _secretHasher.GenerateOpaqueToken();
        var hashedChallenge = _secretHasher.HashToken(rawChallengeToken);

        var bootstrapRecord = new MfaChallengeRecord(
            Guid.NewGuid(),
            hashedChallenge.HashHex,
            hashedChallenge.KeyId,
            MfaChallengePurpose.EnrollmentBootstrap,
            userId,
            securityStamp,
            nameof(MfaMethodType.Authenticator),
            null,
            null,
            0,
            options.Mfa.MaxChallengeAttempts,
            0,
            null,
            nowUtc,
            nowUtc + options.Mfa.ChallengeLifetime);

        await _challengeStore.StoreAsync(bootstrapRecord, cancellationToken).ConfigureAwait(false);

        return JwtServiceResult<JwtLoginResponse>.Success(
            new JwtLoginResponse
            {
                TwoFactorRequired = true,
                MfaEnrollmentRequired = true,
                ExpiresIn = challengeExpiresIn,
                MfaChallengeToken = rawChallengeToken,
                DefaultMethod = nameof(MfaMethodType.Authenticator),
                AvailableMethods = [nameof(MfaMethodType.Authenticator)],
            });
    }

    private async Task<JwtServiceResult<JwtLoginResponse>> CreateEnrolledLoginChallengeAsync(
        TUser user,
        string userId,
        string? securityStamp,
        List<MfaMethodType> enabledMethods,
        DateTimeOffset nowUtc,
        IdentityJwtOptions options,
        CancellationToken cancellationToken)
    {
        var availableMethodNames = enabledMethods.Select(m => m.ToString()).ToList();
        if (options.Mfa.AllowRecoveryCodes && await _userManager.CountRecoveryCodesAsync(user).ConfigureAwait(false) > 0)
        {
            availableMethodNames.Add(nameof(MfaMethodType.RecoveryCode));
        }

        var defaultMethod = enabledMethods[0];
        var challengeId = Guid.NewGuid();
        var rawChallengeToken = _secretHasher.GenerateOpaqueToken();
        var hashedChallenge = _secretHasher.HashToken(rawChallengeToken);
        string? initialOtpHash = null;
        var hashKeyId = hashedChallenge.KeyId;
        DateTimeOffset? resendAvailableAt = null;
        string? rawInitialOtp = null;

        if (defaultMethod is MfaMethodType.Email or MfaMethodType.Sms)
        {
            rawInitialOtp = _secretHasher.GenerateSixDigitCode();
            var otpScope = string.Create(CultureInfo.InvariantCulture, $"login:{challengeId}:{defaultMethod}");
            var otpHashed = _secretHasher.HashScopedCode(otpScope, rawInitialOtp);
            initialOtpHash = otpHashed.HashHex;
            hashKeyId = otpHashed.KeyId;
            resendAvailableAt = nowUtc + options.Mfa.ResendCooldown;
        }

        var loginChallenge = new MfaChallengeRecord(
            challengeId,
            hashedChallenge.HashHex,
            hashKeyId,
            MfaChallengePurpose.LoginChallenge,
            userId,
            securityStamp,
            string.Join(',', availableMethodNames),
            defaultMethod.ToString(),
            initialOtpHash,
            0,
            options.Mfa.MaxChallengeAttempts,
            0,
            resendAvailableAt,
            nowUtc,
            nowUtc + options.Mfa.ChallengeLifetime);

        await _challengeStore.StoreAsync(loginChallenge, cancellationToken).ConfigureAwait(false);
        if (rawInitialOtp != null)
        {
            await DispatchChallengeCodeAsync(user, defaultMethod, rawInitialOtp, cancellationToken).ConfigureAwait(false);
        }

        var challengeExpiresIn = (int)Math.Max(1, Math.Ceiling(options.Mfa.ChallengeLifetime.TotalSeconds));
        return JwtServiceResult<JwtLoginResponse>.Success(
            new JwtLoginResponse
            {
                TwoFactorRequired = true,
                ExpiresIn = challengeExpiresIn,
                MfaChallengeToken = rawChallengeToken,
                DefaultMethod = defaultMethod.ToString(),
                AvailableMethods = availableMethodNames,
            });
    }

    private Task<(bool Verified, string Amr)> VerifySecondFactorCodeAsync(
        TUser user,
        MfaChallengeRecord record,
        MfaMethodType method,
        string rawCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        method switch
        {
            MfaMethodType.Authenticator => VerifyAuthenticatorFactorAsync(user, record, rawCode, nowUtc, cancellationToken),
            MfaMethodType.Email or MfaMethodType.Sms => VerifyOtpFactorAsync(record, method, rawCode, nowUtc, cancellationToken),
            MfaMethodType.RecoveryCode => VerifyRecoveryCodeFactorAsync(user, rawCode),
            _ => Task.FromResult((false, string.Empty)),
        };

    private async Task<(bool Verified, string Amr)> VerifyAuthenticatorFactorAsync(
        TUser user,
        MfaChallengeRecord record,
        string rawCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var secret = await _mfaTokenManager.GetActiveAuthenticatorKeyAsync(user).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(secret) ||
            !TotpCodeHelper.TryValidateCode(secret, rawCode, nowUtc, out var matchedStep))
        {
            return (false, string.Empty);
        }

        var replayScope = string.Create(
            CultureInfo.InvariantCulture,
            $"{record.UserId}:Authenticator:{matchedStep}:{TotpCodeHelper.NormalizeCode(rawCode)}");
        var replayHash = _secretHasher.HashScopedCode("otp-replay", replayScope);
        var window = _optionsMonitor.CurrentValue.Mfa.OtpReplayWindow;
        var recorded = await _challengeStore.TryRecordUsedOtpAsync(
                replayHash.HashHex,
                replayHash.KeyId,
                record.UserId,
                nowUtc,
                nowUtc + window,
                cancellationToken)
            .ConfigureAwait(false);

        return recorded ? (true, "otp") : (false, string.Empty);
    }

    private async Task<(bool Verified, string Amr)> VerifyOtpFactorAsync(
        MfaChallengeRecord record,
        MfaMethodType method,
        string rawCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(record.SelectedMethod, method.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(record.OtpCodeHash))
        {
            return (false, string.Empty);
        }

        var normalized = TotpCodeHelper.NormalizeCode(rawCode);
        var otpScope = string.Create(CultureInfo.InvariantCulture, $"login:{record.ChallengeId}:{method}");
        if (!_secretHasher.VerifyScopedCode(otpScope, normalized, record.OtpCodeHash, record.HashKeyId))
        {
            return (false, string.Empty);
        }

        var replayScope = string.Create(CultureInfo.InvariantCulture, $"{record.UserId}:{method}:{record.ChallengeId}:{normalized}");
        var replayHash = _secretHasher.HashScopedCode("otp-replay", replayScope);
        var recorded = await _challengeStore.TryRecordUsedOtpAsync(
                replayHash.HashHex,
                replayHash.KeyId,
                record.UserId,
                nowUtc,
                record.ExpiresAtUtc,
                cancellationToken)
            .ConfigureAwait(false);

        var amr = method == MfaMethodType.Email ? "email" : "sms";
        return recorded ? (true, amr) : (false, string.Empty);
    }

    private async Task<(bool Verified, string Amr)> VerifyRecoveryCodeFactorAsync(
        TUser user,
        string rawCode)
    {
        if (!_optionsMonitor.CurrentValue.Mfa.AllowRecoveryCodes)
        {
            return (false, string.Empty);
        }

        var candidate = rawCode.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        var redeemResult = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, candidate).ConfigureAwait(false);
        return redeemResult.Succeeded ? (true, "recovery") : (false, string.Empty);
    }

    private async Task DispatchChallengeCodeAsync(
        TUser user,
        MfaMethodType method,
        string rawOtp,
        CancellationToken cancellationToken)
    {
        if (method == MfaMethodType.Email)
        {
            var email = await _userManager.GetEmailAsync(user).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(email))
            {
                await _mfaCodeSender.SendEmailCodeAsync(user, email, rawOtp, cancellationToken).ConfigureAwait(false);
            }
        }
        else if (method == MfaMethodType.Sms)
        {
            var phone = await _userManager.GetPhoneNumberAsync(user).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(phone))
            {
                await _mfaCodeSender.SendSmsCodeAsync(user, phone, rawOtp, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<bool> IsAccountEligibleAsync(
        TUser user,
        string? expectedSecurityStamp)
    {
        if (_userManager.SupportsUserSecurityStamp)
        {
            var currentStamp = await _userManager.GetSecurityStampAsync(user).ConfigureAwait(false);
            if (!string.Equals(currentStamp, expectedSecurityStamp, StringComparison.Ordinal))
            {
                return false;
            }
        }

        if (!await _signInManager.CanSignInAsync(user).ConfigureAwait(false))
        {
            return false;
        }

        return !_userManager.SupportsUserLockout || !await _userManager.IsLockedOutAsync(user).ConfigureAwait(false);
    }

    private async Task HandleReplayCascadeAsync(
        string sessionId,
        DateTimeOffset nowUtc,
        IdentityJwtOptions options,
        CancellationToken cancellationToken)
    {
        await _refreshTokenStore
            .RevokeSessionAsync(sessionId, nowUtc, "refresh_token_replay", cancellationToken)
            .ConfigureAwait(false);

        await _revocationStore.RevokeAsync(
                new TokenRevocationEntry(
                    options.Issuer,
                    RevocationSubjectType.SessionId,
                    sessionId,
                    nowUtc,
                    nowUtc + options.AccessTokenLifetime + options.ClockSkew,
                    null,
                    "refresh_token_replay"),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
