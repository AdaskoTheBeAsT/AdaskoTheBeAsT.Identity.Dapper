using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

internal sealed class IdentityJwtMfaService<TUser> : IIdentityJwtMfaService<TUser>
    where TUser : class
{
    private readonly UserManager<TUser> _userManager;
    private readonly MfaUserTokenManager<TUser> _tokenManager;
    private readonly IMfaChallengeStore _challengeStore;
    private readonly ITokenSecretHasher _secretHasher;
    private readonly IMfaCodeSender<TUser> _codeSender;
    private readonly JwtSessionIssuer<TUser> _sessionIssuer;
    private readonly TimeProvider _timeProvider;
    private readonly IOptionsMonitor<IdentityJwtOptions> _optionsMonitor;
    private readonly IMfaQrCodeGenerator? _qrCodeGenerator;

    public IdentityJwtMfaService(
        UserManager<TUser> userManager,
        MfaUserTokenManager<TUser> tokenManager,
        IMfaChallengeStore challengeStore,
        ITokenSecretHasher secretHasher,
        IMfaCodeSender<TUser> codeSender,
        JwtSessionIssuer<TUser> sessionIssuer,
        TimeProvider timeProvider,
        IOptionsMonitor<IdentityJwtOptions> optionsMonitor,
        IMfaQrCodeGenerator? qrCodeGenerator = null)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(tokenManager);
        ArgumentNullException.ThrowIfNull(challengeStore);
        ArgumentNullException.ThrowIfNull(secretHasher);
        ArgumentNullException.ThrowIfNull(codeSender);
        ArgumentNullException.ThrowIfNull(sessionIssuer);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _userManager = userManager;
        _tokenManager = tokenManager;
        _challengeStore = challengeStore;
        _secretHasher = secretHasher;
        _codeSender = codeSender;
        _sessionIssuer = sessionIssuer;
        _timeProvider = timeProvider;
        _optionsMonitor = optionsMonitor;
        _qrCodeGenerator = qrCodeGenerator;
    }

    public async Task<JwtServiceResult<JwtMfaStatusResponse>> GetStatusAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await ResolveAuthenticatedUserAsync(principal).ConfigureAwait(false);
        if (user == null)
        {
            return JwtServiceResult<JwtMfaStatusResponse>.Fail(JwtOperationStatus.Unauthorized, "unauthorized");
        }

        var twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
        var activeKey = await _tokenManager.GetActiveAuthenticatorKeyAsync(user).ConfigureAwait(false);
        var recoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user).ConfigureAwait(false);
        var enabledMethods = await _tokenManager.GetEnabledMethodsAsync(user).ConfigureAwait(false);

        return JwtServiceResult<JwtMfaStatusResponse>.Success(
            new JwtMfaStatusResponse(
                twoFactorEnabled,
                !string.IsNullOrWhiteSpace(activeKey),
                recoveryCodesLeft,
                enabledMethods.Select(m => m.ToString()).ToArray()));
    }

    public async Task<JwtServiceResult<JwtMfaAuthenticatorSetupResponse>> SetupAuthenticatorAsync(
        ClaimsPrincipal? principal,
        JwtMfaAuthenticatorSetupRequest? request,
        CancellationToken cancellationToken)
    {
        var (user, _, errorResult) = await ResolveUserOrEnrollmentChallengeAsync<JwtMfaAuthenticatorSetupResponse>(
                principal,
                request?.MfaChallengeToken,
                incrementAttempt: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (user == null)
        {
            return errorResult!;
        }

        var response = await StageAuthenticatorForUserAsync(user).ConfigureAwait(false);
        return JwtServiceResult<JwtMfaAuthenticatorSetupResponse>.Success(response);
    }

    public async Task<JwtServiceResult<JwtMfaAuthenticatorConfirmResponse>> ConfirmAuthenticatorAsync(
        ClaimsPrincipal? principal,
        JwtMfaAuthenticatorConfirmRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return JwtServiceResult<JwtMfaAuthenticatorConfirmResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_request",
                "Verification code is required.");
        }

        var (user, challengeHash, errorResult) = await ResolveUserOrEnrollmentChallengeAsync<JwtMfaAuthenticatorConfirmResponse>(
                principal,
                request.MfaChallengeToken,
                incrementAttempt: true,
                cancellationToken)
            .ConfigureAwait(false);

        if (user == null)
        {
            return errorResult!;
        }

        var pendingKey = await _tokenManager.GetPendingAuthenticatorKeyAsync(user).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(pendingKey))
        {
            return JwtServiceResult<JwtMfaAuthenticatorConfirmResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "authenticator_setup_not_started",
                "Start authenticator setup before confirming.");
        }

        return await VerifyAndPromoteAuthenticatorAsync(user, pendingKey, request.Code, challengeHash, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<JwtServiceResult<JwtMfaMethodUpdateResponse>> UpdateMethodAsync(
        ClaimsPrincipal principal,
        JwtMfaMethodUpdateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await ResolveAuthenticatedUserAsync(principal).ConfigureAwait(false);
        if (user == null)
        {
            return JwtServiceResult<JwtMfaMethodUpdateResponse>.Fail(JwtOperationStatus.Unauthorized, "unauthorized");
        }

        if (!Enum.TryParse<MfaMethodType>(request.Method, ignoreCase: true, out var method) ||
            method is not (MfaMethodType.Email or MfaMethodType.Sms))
        {
            return JwtServiceResult<JwtMfaMethodUpdateResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_method",
                "Only Email or Sms methods can be toggled via this endpoint.");
        }

        var options = _optionsMonitor.CurrentValue;
        if ((method == MfaMethodType.Email && !options.Mfa.AllowEmailMfa) ||
            (method == MfaMethodType.Sms && !options.Mfa.AllowSmsMfa))
        {
            return JwtServiceResult<JwtMfaMethodUpdateResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "method_disabled",
                "Requested MFA method is not enabled on this server.");
        }

        var twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
        if (twoFactorEnabled && !JwtSessionIssuer<TUser>.HasMfaAssurance(principal))
        {
            return JwtServiceResult<JwtMfaMethodUpdateResponse>.Fail(
                JwtOperationStatus.Forbidden,
                "mfa_assurance_required",
                "Modifying MFA methods requires an MFA-authenticated session.");
        }

        return request.Enabled
            ? await EnableSecondaryMethodAsync(user, method, request, options, cancellationToken).ConfigureAwait(false)
            : await DisableSecondaryMethodAsync(user, method, options).ConfigureAwait(false);
    }

    public async Task<JwtServiceResult<JwtMfaRecoveryCodesResponse>> RegenerateRecoveryCodesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await ResolveAuthenticatedUserAsync(principal).ConfigureAwait(false);
        if (user == null)
        {
            return JwtServiceResult<JwtMfaRecoveryCodesResponse>.Fail(JwtOperationStatus.Unauthorized, "unauthorized");
        }

        if (!JwtSessionIssuer<TUser>.HasMfaAssurance(principal))
        {
            return JwtServiceResult<JwtMfaRecoveryCodesResponse>.Fail(
                JwtOperationStatus.Forbidden,
                "mfa_assurance_required",
                "Regenerating recovery codes requires an MFA-authenticated session.");
        }

        if (!await _userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false))
        {
            return JwtServiceResult<JwtMfaRecoveryCodesResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "mfa_not_enabled",
                "Two-factor authentication must be enabled before generating recovery codes.");
        }

        var codes = await _userManager
            .GenerateNewTwoFactorRecoveryCodesAsync(user, _optionsMonitor.CurrentValue.Mfa.RecoveryCodeCount)
            .ConfigureAwait(false);

        return JwtServiceResult<JwtMfaRecoveryCodesResponse>.Success(
            new JwtMfaRecoveryCodesResponse(codes?.ToArray() ?? []));
    }

    public async Task<JwtServiceResult<bool>> DisableMfaAsync(
        ClaimsPrincipal principal,
        JwtMfaDisableRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await ResolveAuthenticatedUserAsync(principal).ConfigureAwait(false);
        if (user == null)
        {
            return JwtServiceResult<bool>.Fail(JwtOperationStatus.Unauthorized, "unauthorized");
        }

        var options = _optionsMonitor.CurrentValue;
        if (options.Mfa.EnforcementMode == MfaEnforcementMode.RequiredForAllUsers)
        {
            return JwtServiceResult<bool>.Fail(
                JwtOperationStatus.InvalidRequest,
                "mfa_required_by_policy",
                "Multi-factor authentication is mandatory and cannot be disabled.");
        }

        var hasMfaClaim = JwtSessionIssuer<TUser>.HasMfaAssurance(principal);
        var passwordVerified = !string.IsNullOrWhiteSpace(request.Password) &&
                               await _userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);

        if (!hasMfaClaim && !passwordVerified)
        {
            return JwtServiceResult<bool>.Fail(
                JwtOperationStatus.Forbidden,
                "step_up_required",
                "Disabling MFA requires an MFA-authenticated session or current password confirmation.");
        }

        await _tokenManager.ClearAllMfaStateAsync(user, cancellationToken).ConfigureAwait(false);

        if (request.RevokeOtherSessions)
        {
            var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
            await _sessionIssuer.RevokeAllUserSessionsAsync(
                    userId,
                    JwtSessionIssuer<TUser>.ExtractSessionId(principal),
                    JwtSessionIssuer<TUser>.ExtractJwtId(principal),
                    "MfaDisabled",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return JwtServiceResult<bool>.Success(true);
    }

    public async Task<JwtServiceResult<JwtMfaSetupLinkResponse>> SendSetupLinkAsync(
        ClaimsPrincipal principal,
        JwtMfaSetupLinkRequest? request,
        CancellationToken cancellationToken)
    {
        var user = await ResolveAuthenticatedUserAsync(principal).ConfigureAwait(false);
        if (user == null)
        {
            return JwtServiceResult<JwtMfaSetupLinkResponse>.Fail(JwtOperationStatus.Unauthorized, "unauthorized");
        }

        var options = _optionsMonitor.CurrentValue;
        if (options.Mfa.PublicSetupBaseUri == null)
        {
            return JwtServiceResult<JwtMfaSetupLinkResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "setup_link_not_configured",
                "PublicSetupBaseUri is not configured.");
        }

        var email = await _userManager.GetEmailAsync(user).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(email))
        {
            return JwtServiceResult<JwtMfaSetupLinkResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "email_missing",
                "User does not have an email address configured.");
        }

        return await CreateAndSendSetupLinkAsync(user, email, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JwtServiceResult<JwtMfaSetupLinkExchangeResponse>> ExchangeSetupLinkAsync(
        JwtMfaSetupLinkExchangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.SetupToken))
        {
            return JwtServiceResult<JwtMfaSetupLinkExchangeResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_request",
                "Setup token is required.");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var matchedRecord = await TryConsumeSetupLinkTokenAsync(request.SetupToken, nowUtc, cancellationToken).ConfigureAwait(false);
        if (matchedRecord == null)
        {
            return JwtServiceResult<JwtMfaSetupLinkExchangeResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_setup_token",
                "Setup token is invalid or expired.");
        }

        var user = await _userManager.FindByIdAsync(matchedRecord.UserId).ConfigureAwait(false);
        if (user == null || !await SecurityStampMatchesAsync(user, matchedRecord.SecurityStamp).ConfigureAwait(false))
        {
            return JwtServiceResult<JwtMfaSetupLinkExchangeResponse>.Fail(
                JwtOperationStatus.Unauthorized,
                "invalid_setup_token",
                "Setup token is no longer valid.");
        }

        return await IssueBootstrapFromSetupLinkAsync(user, matchedRecord, nowUtc, cancellationToken).ConfigureAwait(false);
    }

    private static Uri BuildSetupUri(
        Uri publicBaseUri,
        string rawSetupToken)
    {
        var uriBuilder = new UriBuilder(publicBaseUri);
        var existingQuery = uriBuilder.Query.TrimStart('?');
        var tokenParam = $"token={Uri.EscapeDataString(rawSetupToken)}";
        uriBuilder.Query = string.IsNullOrEmpty(existingQuery) ? tokenParam : $"{existingQuery}&{tokenParam}";
        return uriBuilder.Uri;
    }

    private async Task<JwtServiceResult<JwtMfaAuthenticatorConfirmResponse>> VerifyAndPromoteAuthenticatorAsync(
        TUser user,
        string pendingKey,
        string rawCode,
        string? challengeHash,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        if (!TotpCodeHelper.TryValidateCode(pendingKey, rawCode, nowUtc, out var matchedStep))
        {
            return JwtServiceResult<JwtMfaAuthenticatorConfirmResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_mfa_code",
                "Invalid verification code.");
        }

        var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
        var replayScope = string.Create(
            CultureInfo.InvariantCulture,
            $"{userId}:Authenticator:{matchedStep}:{TotpCodeHelper.NormalizeCode(rawCode)}");
        var replayHash = _secretHasher.HashScopedCode("otp-replay", replayScope);
        var window = _optionsMonitor.CurrentValue.Mfa.OtpReplayWindow;

        var recorded = await _challengeStore.TryRecordUsedOtpAsync(
                replayHash.HashHex,
                replayHash.KeyId,
                userId,
                nowUtc,
                nowUtc + window,
                cancellationToken)
            .ConfigureAwait(false);

        if (!recorded)
        {
            return JwtServiceResult<JwtMfaAuthenticatorConfirmResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_mfa_code",
                "Verification code was already used.");
        }

        return await ActivateAuthenticatorAndSessionAsync(user, pendingKey, challengeHash, nowUtc, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<JwtServiceResult<JwtMfaAuthenticatorConfirmResponse>> ActivateAuthenticatorAndSessionAsync(
        TUser user,
        string pendingKey,
        string? challengeHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        await _tokenManager.SetActiveAuthenticatorKeyAsync(user, pendingKey).ConfigureAwait(false);
        await _tokenManager.RemovePendingAuthenticatorKeyAsync(user).ConfigureAwait(false);

        var methods = (await _tokenManager.GetEnabledMethodsAsync(user).ConfigureAwait(false)).ToList();
        if (!methods.Contains(MfaMethodType.Authenticator))
        {
            methods.Insert(0, MfaMethodType.Authenticator);
        }

        await _tokenManager.SetEnabledMethodsAsync(user, methods).ConfigureAwait(false);
        await _userManager.SetTwoFactorEnabledAsync(user, enabled: true).ConfigureAwait(false);

        IReadOnlyList<string> recoveryCodes = [];
        if (await _userManager.CountRecoveryCodesAsync(user).ConfigureAwait(false) == 0)
        {
            var generated = await _userManager
                .GenerateNewTwoFactorRecoveryCodesAsync(user, _optionsMonitor.CurrentValue.Mfa.RecoveryCodeCount)
                .ConfigureAwait(false);
            recoveryCodes = generated?.ToArray() ?? [];
        }

        JwtLoginResponse? issuedSession = null;
        if (!string.IsNullOrWhiteSpace(challengeHash))
        {
            await _challengeStore.ConsumeAsync(challengeHash, nowUtc, cancellationToken).ConfigureAwait(false);
            issuedSession = await _sessionIssuer.IssueNewSessionAsync(user, ["pwd", "mfa", "otp"], cancellationToken)
                .ConfigureAwait(false);
        }

        return JwtServiceResult<JwtMfaAuthenticatorConfirmResponse>.Success(
            new JwtMfaAuthenticatorConfirmResponse(TwoFactorEnabled: true, recoveryCodes, issuedSession));
    }

    private async Task<JwtServiceResult<JwtMfaSetupLinkResponse>> CreateAndSendSetupLinkAsync(
        TUser user,
        string email,
        IdentityJwtOptions options,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var rawSetupToken = _secretHasher.GenerateOpaqueToken();
        var hashedToken = _secretHasher.HashToken(rawSetupToken);
        var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
        var securityStamp = _userManager.SupportsUserSecurityStamp
            ? await _userManager.GetSecurityStampAsync(user).ConfigureAwait(false)
            : null;

        var record = new MfaChallengeRecord(
            Guid.NewGuid(),
            hashedToken.HashHex,
            hashedToken.KeyId,
            MfaChallengePurpose.SetupLink,
            userId,
            securityStamp,
            nameof(MfaMethodType.Authenticator),
            null,
            null,
            0,
            1,
            0,
            null,
            nowUtc,
            nowUtc + options.Mfa.SetupLinkLifetime);

        await _challengeStore.StoreAsync(record, cancellationToken).ConfigureAwait(false);
        var setupUri = BuildSetupUri(options.Mfa.PublicSetupBaseUri!, rawSetupToken);
        await _codeSender.SendSetupLinkAsync(user, email, setupUri, cancellationToken).ConfigureAwait(false);

        var expiresIn = (int)Math.Max(1, Math.Ceiling(options.Mfa.SetupLinkLifetime.TotalSeconds));
        return JwtServiceResult<JwtMfaSetupLinkResponse>.Success(new JwtMfaSetupLinkResponse(Sent: true, expiresIn));
    }

    private async Task<MfaChallengeRecord?> TryConsumeSetupLinkTokenAsync(
        string rawSetupToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        foreach (var hashHex in _secretHasher.ComputeCandidateTokenHashes(rawSetupToken).Select(candidate => candidate.HashHex))
        {
            var found = await _challengeStore.FindAsync(hashHex, nowUtc, cancellationToken).ConfigureAwait(false);
            if (found is { Purpose: MfaChallengePurpose.SetupLink })
            {
                return await _challengeStore.ConsumeAsync(hashHex, nowUtc, cancellationToken).ConfigureAwait(false)
                    ? found
                    : null;
            }
        }

        return null;
    }

    private async Task<JwtServiceResult<JwtMfaSetupLinkExchangeResponse>> IssueBootstrapFromSetupLinkAsync(
        TUser user,
        MfaChallengeRecord matchedRecord,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var rawBootstrapToken = _secretHasher.GenerateOpaqueToken();
        var hashedBootstrap = _secretHasher.HashToken(rawBootstrapToken);
        var bootstrapRecord = new MfaChallengeRecord(
            Guid.NewGuid(),
            hashedBootstrap.HashHex,
            hashedBootstrap.KeyId,
            MfaChallengePurpose.EnrollmentBootstrap,
            matchedRecord.UserId,
            matchedRecord.SecurityStamp,
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
        var setup = await StageAuthenticatorForUserAsync(user).ConfigureAwait(false);
        var expiresIn = (int)Math.Max(1, Math.Ceiling(options.Mfa.ChallengeLifetime.TotalSeconds));

        return JwtServiceResult<JwtMfaSetupLinkExchangeResponse>.Success(
            new JwtMfaSetupLinkExchangeResponse(rawBootstrapToken, expiresIn, setup));
    }

    private async Task<JwtMfaAuthenticatorSetupResponse> StageAuthenticatorForUserAsync(TUser user)
    {
        var options = _optionsMonitor.CurrentValue;
        var rawSecret = TotpCodeHelper.GenerateSecretBase32();
        await _tokenManager.SetPendingAuthenticatorKeyAsync(user, rawSecret).ConfigureAwait(false);

        var accountName = await _userManager.GetEmailAsync(user).ConfigureAwait(false)
                          ?? await _userManager.GetUserNameAsync(user).ConfigureAwait(false)
                          ?? await _userManager.GetUserIdAsync(user).ConfigureAwait(false);

        var issuer = string.IsNullOrWhiteSpace(options.Mfa.AuthenticatorIssuer)
            ? options.Issuer
            : options.Mfa.AuthenticatorIssuer;

        var otpAuthUri = TotpCodeHelper.BuildOtpAuthUri(issuer, accountName, rawSecret);
        var sharedKey = TotpCodeHelper.FormatKeyForManualEntry(rawSecret);
        var qrCodeSvg = _qrCodeGenerator?.GenerateSvg(otpAuthUri);

        return new JwtMfaAuthenticatorSetupResponse(sharedKey, otpAuthUri, qrCodeSvg);
    }

    private async Task<JwtServiceResult<JwtMfaMethodUpdateResponse>> DisableSecondaryMethodAsync(
        TUser user,
        MfaMethodType method,
        IdentityJwtOptions options)
    {
        var currentMethods = (await _tokenManager.GetEnabledMethodsAsync(user).ConfigureAwait(false)).ToList();
        currentMethods.Remove(method);

        if (currentMethods.Count == 0 && options.Mfa.EnforcementMode == MfaEnforcementMode.RequiredForAllUsers)
        {
            return JwtServiceResult<JwtMfaMethodUpdateResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "mfa_required_by_policy",
                "Cannot disable the last remaining MFA method when MFA is mandatory.");
        }

        await _tokenManager.SetEnabledMethodsAsync(user, currentMethods).ConfigureAwait(false);
        if (currentMethods.Count == 0)
        {
            await _userManager.SetTwoFactorEnabledAsync(user, enabled: false).ConfigureAwait(false);
        }

        return JwtServiceResult<JwtMfaMethodUpdateResponse>.Success(
            new JwtMfaMethodUpdateResponse(
                VerificationRequired: false,
                currentMethods.Select(m => m.ToString()).ToArray()));
    }

    private async Task<JwtServiceResult<JwtMfaMethodUpdateResponse>> EnableSecondaryMethodAsync(
        TUser user,
        MfaMethodType method,
        JwtMfaMethodUpdateRequest request,
        IdentityJwtOptions options,
        CancellationToken cancellationToken)
    {
        var (destination, destinationError) = await ValidateSecondaryDestinationAsync(user, method).ConfigureAwait(false);
        if (destinationError != null)
        {
            return destinationError;
        }

        var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(request.VerificationCode)
            ? await SendSecondaryEnrollmentCodeAsync(user, userId, method, destination!, options, cancellationToken)
                .ConfigureAwait(false)
            : await ConfirmSecondaryEnrollmentCodeAsync(user, userId, method, request, cancellationToken)
                .ConfigureAwait(false);
    }

    private async Task<(string? Destination, JwtServiceResult<JwtMfaMethodUpdateResponse>? Error)> ValidateSecondaryDestinationAsync(
        TUser user,
        MfaMethodType method)
    {
        if (method == MfaMethodType.Email)
        {
            var email = await _userManager.GetEmailAsync(user).ConfigureAwait(false);
            var confirmed = await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(email) || !confirmed
                ? (null, JwtServiceResult<JwtMfaMethodUpdateResponse>.Fail(
                    JwtOperationStatus.InvalidRequest,
                    "email_not_confirmed",
                    "A confirmed email address is required to enable Email MFA."))
                : (email, null);
        }

        var phone = await _userManager.GetPhoneNumberAsync(user).ConfigureAwait(false);
        var phoneConfirmed = await _userManager.IsPhoneNumberConfirmedAsync(user).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(phone) || !phoneConfirmed
            ? (null, JwtServiceResult<JwtMfaMethodUpdateResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "phone_not_confirmed",
                "A confirmed phone number is required to enable SMS MFA."))
            : (phone, null);
    }

    private async Task<JwtServiceResult<JwtMfaMethodUpdateResponse>> SendSecondaryEnrollmentCodeAsync(
        TUser user,
        string userId,
        MfaMethodType method,
        string destination,
        IdentityJwtOptions options,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var rawChallengeToken = _secretHasher.GenerateOpaqueToken();
        var hashedChallenge = _secretHasher.HashToken(rawChallengeToken);
        var rawOtp = _secretHasher.GenerateSixDigitCode();
        var otpScope = string.Create(CultureInfo.InvariantCulture, $"enroll:{userId}:{method}");
        var hashedOtp = _secretHasher.HashScopedCode(otpScope, rawOtp);
        var securityStamp = _userManager.SupportsUserSecurityStamp
            ? await _userManager.GetSecurityStampAsync(user).ConfigureAwait(false)
            : null;

        var record = new MfaChallengeRecord(
            Guid.NewGuid(),
            hashedChallenge.HashHex,
            hashedOtp.KeyId,
            MfaChallengePurpose.EnrollmentBootstrap,
            userId,
            securityStamp,
            method.ToString(),
            method.ToString(),
            hashedOtp.HashHex,
            0,
            options.Mfa.MaxChallengeAttempts,
            0,
            nowUtc + options.Mfa.ResendCooldown,
            nowUtc,
            nowUtc + options.Mfa.ChallengeLifetime);

        await _challengeStore.StoreAsync(record, cancellationToken).ConfigureAwait(false);

        if (method == MfaMethodType.Email)
        {
            await _codeSender.SendEmailCodeAsync(user, destination, rawOtp, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _codeSender.SendSmsCodeAsync(user, destination, rawOtp, cancellationToken).ConfigureAwait(false);
        }

        var current = await _tokenManager.GetEnabledMethodsAsync(user).ConfigureAwait(false);
        return JwtServiceResult<JwtMfaMethodUpdateResponse>.Success(
            new JwtMfaMethodUpdateResponse(
                VerificationRequired: true,
                current.Select(m => m.ToString()).ToArray(),
                rawChallengeToken));
    }

    private async Task<JwtServiceResult<JwtMfaMethodUpdateResponse>> ConfirmSecondaryEnrollmentCodeAsync(
        TUser user,
        string userId,
        MfaMethodType method,
        JwtMfaMethodUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeToken))
        {
            return JwtServiceResult<JwtMfaMethodUpdateResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "challenge_token_required",
                "Challenge token is required to confirm secondary MFA method.");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var challengeDigest = _secretHasher.HashToken(request.ChallengeToken).HashHex;
        var attempt = await _challengeStore
            .IncrementAttemptAsync(challengeDigest, nowUtc, cancellationToken)
            .ConfigureAwait(false);

        if (attempt.Status != MfaChallengeAttemptStatus.Allowed ||
            attempt.Record is not { Purpose: MfaChallengePurpose.EnrollmentBootstrap } record ||
            !string.Equals(record.UserId, userId, StringComparison.Ordinal) ||
            !string.Equals(record.SelectedMethod, method.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(record.OtpCodeHash))
        {
            return JwtServiceResult<JwtMfaMethodUpdateResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_mfa_challenge",
                "Verification challenge is invalid or expired.");
        }

        var verifyScope = string.Create(CultureInfo.InvariantCulture, $"enroll:{userId}:{method}");
        var normalizedOtp = TotpCodeHelper.NormalizeCode(request.VerificationCode);
        if (!_secretHasher.VerifyScopedCode(verifyScope, normalizedOtp, record.OtpCodeHash, record.HashKeyId))
        {
            return JwtServiceResult<JwtMfaMethodUpdateResponse>.Fail(
                JwtOperationStatus.InvalidRequest,
                "invalid_mfa_code",
                "Invalid verification code.");
        }

        await _challengeStore.ConsumeAsync(challengeDigest, nowUtc, cancellationToken).ConfigureAwait(false);

        var updatedMethods = (await _tokenManager.GetEnabledMethodsAsync(user).ConfigureAwait(false)).ToList();
        if (!updatedMethods.Contains(method))
        {
            updatedMethods.Add(method);
        }

        await _tokenManager.SetEnabledMethodsAsync(user, updatedMethods).ConfigureAwait(false);
        await _userManager.SetTwoFactorEnabledAsync(user, enabled: true).ConfigureAwait(false);

        return JwtServiceResult<JwtMfaMethodUpdateResponse>.Success(
            new JwtMfaMethodUpdateResponse(
                VerificationRequired: false,
                updatedMethods.Select(m => m.ToString()).ToArray()));
    }

    private async Task<TUser?> ResolveAuthenticatedUserAsync(ClaimsPrincipal? principal)
    {
        var userId = JwtSessionIssuer<TUser>.ExtractUserId(principal);
        return string.IsNullOrWhiteSpace(userId)
            ? null
            : await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
    }

    private async Task<(TUser? User, string? ChallengeDigest, JwtServiceResult<TResponse>? Error)> ResolveUserOrEnrollmentChallengeAsync<TResponse>(
        ClaimsPrincipal? principal,
        string? rawChallengeToken,
        bool incrementAttempt,
        CancellationToken cancellationToken)
    {
        var authenticatedUser = await ResolveAuthenticatedUserAsync(principal).ConfigureAwait(false);
        if (authenticatedUser != null)
        {
            return (authenticatedUser, null, null);
        }

        if (string.IsNullOrWhiteSpace(rawChallengeToken))
        {
            return (null, null, JwtServiceResult<TResponse>.Fail(JwtOperationStatus.Unauthorized, "unauthorized"));
        }

        return await LookupEnrollmentChallengeUserAsync<TResponse>(rawChallengeToken, incrementAttempt, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<(TUser? User, string? ChallengeDigest, JwtServiceResult<TResponse>? Error)> LookupEnrollmentChallengeUserAsync<TResponse>(
        string rawChallengeToken,
        bool incrementAttempt,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        foreach (var hashHex in _secretHasher.ComputeCandidateTokenHashes(rawChallengeToken).Select(candidate => candidate.HashHex))
        {
            var (status, record) = incrementAttempt
                ? await _challengeStore.IncrementAttemptAsync(hashHex, nowUtc, cancellationToken).ConfigureAwait(false)
                : new MfaChallengeAttemptResult(
                    MfaChallengeAttemptStatus.Allowed,
                    await _challengeStore.FindAsync(hashHex, nowUtc, cancellationToken).ConfigureAwait(false));

            if (status == MfaChallengeAttemptStatus.NotFoundOrExpired || record == null)
            {
                continue;
            }

            if (status == MfaChallengeAttemptStatus.Exhausted)
            {
                await RecordExhaustedChallengeFailureAsync(record.UserId).ConfigureAwait(false);
                return (
                    null,
                    null,
                    JwtServiceResult<TResponse>.Fail(
                        JwtOperationStatus.Unauthorized,
                        "mfa_challenge_exhausted",
                        "Maximum verification attempts exceeded."));
            }

            if (record.Purpose != MfaChallengePurpose.EnrollmentBootstrap)
            {
                continue;
            }

            var user = await _userManager.FindByIdAsync(record.UserId).ConfigureAwait(false);
            return user != null && await SecurityStampMatchesAsync(user, record.SecurityStamp).ConfigureAwait(false)
                ? (user, hashHex, null)
                : (null, null, JwtServiceResult<TResponse>.Fail(JwtOperationStatus.Unauthorized, "invalid_mfa_challenge"));
        }

        return (null, null, JwtServiceResult<TResponse>.Fail(JwtOperationStatus.Unauthorized, "invalid_mfa_challenge"));
    }

    private async Task RecordExhaustedChallengeFailureAsync(string userId)
    {
        if (!_userManager.SupportsUserLockout)
        {
            return;
        }

        var exhaustedUser = await _userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (exhaustedUser != null)
        {
            await _userManager.AccessFailedAsync(exhaustedUser).ConfigureAwait(false);
        }
    }

    private async Task<bool> SecurityStampMatchesAsync(
        TUser user,
        string? expectedStamp)
    {
        if (!_userManager.SupportsUserSecurityStamp)
        {
            return true;
        }

        var currentStamp = await _userManager.GetSecurityStampAsync(user).ConfigureAwait(false);
        return string.Equals(currentStamp, expectedStamp, StringComparison.Ordinal);
    }
}
