using System;
using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

public sealed class IdentityJwtOptionsValidator : IValidateOptions<IdentityJwtOptions>
{
    private readonly IHostEnvironment? _environment;

    public IdentityJwtOptionsValidator(IHostEnvironment? environment = null)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(
        string? name,
        IdentityJwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = IdentityJwtValidationOptionsValidator.ValidateCore(
            options,
            _environment,
            requireActiveSigningKey: true);

        var isDevelopment = _environment?.IsDevelopment() == true;
        ValidateLifetimes(options, failures);
        ValidateTokenHashKeys(options, isDevelopment, failures);
        ValidateMfaOptions(options.Mfa, isDevelopment, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateLifetimes(
        IdentityJwtOptions options,
        List<string> failures)
    {
        if (options.AccessTokenLifetime <= TimeSpan.Zero)
        {
            failures.Add("AccessTokenLifetime must be positive.");
        }

        if (options.RefreshTokenLifetime <= TimeSpan.Zero)
        {
            failures.Add("RefreshTokenLifetime must be positive.");
        }

        if (options.AbsoluteSessionLifetime < options.RefreshTokenLifetime)
        {
            failures.Add("AbsoluteSessionLifetime must be greater than or equal to RefreshTokenLifetime.");
        }

        if (options.InMemoryRefreshTokenCapacity <= 0)
        {
            failures.Add("InMemoryRefreshTokenCapacity must be positive.");
        }

        if (options.InMemoryRevocationCapacity <= 0)
        {
            failures.Add("InMemoryRevocationCapacity must be positive.");
        }
    }

    private static void ValidateMfaOptions(
        IdentityJwtMfaOptions mfa,
        bool isDevelopment,
        List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(mfa.AuthenticatorIssuer))
        {
            failures.Add("Mfa.AuthenticatorIssuer must be specified.");
        }

        if (mfa.ChallengeLifetime <= TimeSpan.Zero || mfa.ChallengeLifetime > TimeSpan.FromHours(1))
        {
            failures.Add("Mfa.ChallengeLifetime must be positive and at most 1 hour.");
        }

        if (mfa.MaxChallengeAttempts <= 0 || mfa.MaxChallengeAttempts > 20)
        {
            failures.Add("Mfa.MaxChallengeAttempts must be between 1 and 20.");
        }

        if (mfa.ResendCooldown < TimeSpan.Zero)
        {
            failures.Add("Mfa.ResendCooldown cannot be negative.");
        }

        if (mfa.MaxResendCount < 0)
        {
            failures.Add("Mfa.MaxResendCount cannot be negative.");
        }

        if (mfa.OtpReplayWindow < TimeSpan.FromSeconds(30))
        {
            failures.Add("Mfa.OtpReplayWindow must be at least 30 seconds.");
        }

        if (mfa.RecoveryCodeCount <= 0 || mfa.RecoveryCodeCount > 50)
        {
            failures.Add("Mfa.RecoveryCodeCount must be between 1 and 50.");
        }

        ValidateSetupBaseUri(mfa.PublicSetupBaseUri, isDevelopment, failures);
    }

    private static void ValidateSetupBaseUri(
        Uri? setupBaseUri,
        bool isDevelopment,
        List<string> failures)
    {
        if (setupBaseUri == null)
        {
            return;
        }

        if (!setupBaseUri.IsAbsoluteUri)
        {
            failures.Add("Mfa.PublicSetupBaseUri must be an absolute URI when configured.");
            return;
        }

        if (!isDevelopment &&
            !string.Equals(setupBaseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("Mfa.PublicSetupBaseUri must use HTTPS outside Development.");
        }
    }

    private static void ValidateTokenHashKeys(
        IdentityJwtOptions options,
        bool isDevelopment,
        List<string> failures)
    {
        var hasValidHashKey = !string.IsNullOrWhiteSpace(options.TokenHashKey) &&
                              Encoding.UTF8.GetByteCount(options.TokenHashKey) >= 32;

        if (!hasValidHashKey)
        {
            if (options.AllowUnkeyedSha256InDevelopment &&
                isDevelopment &&
                !options.Mfa.AllowEmailMfa &&
                !options.Mfa.AllowSmsMfa)
            {
                return;
            }

            failures.Add(
                "TokenHashKey must contain at least 32 UTF-8 bytes (required for HMAC-SHA-256 token and OTP hashing).");
            return;
        }

        if (!isDevelopment &&
            JwtKeyMaterialHelper.IsDemoKey(Encoding.UTF8.GetBytes(options.TokenHashKey!)))
        {
            failures.Add(
                "The public demo key cannot be used as TokenHashKey outside Development.");
        }
    }
}
