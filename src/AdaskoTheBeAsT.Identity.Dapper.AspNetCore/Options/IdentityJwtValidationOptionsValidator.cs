using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

public sealed class IdentityJwtValidationOptionsValidator : IValidateOptions<IdentityJwtValidationOptions>
{
    private readonly IHostEnvironment? _environment;

    public IdentityJwtValidationOptionsValidator(IHostEnvironment? environment = null)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(
        string? name,
        IdentityJwtValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = ValidateCore(options, _environment, requireActiveSigningKey: false);
        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    internal static List<string> ValidateCore(
        IdentityJwtValidationOptions options,
        IHostEnvironment? environment,
        bool requireActiveSigningKey)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.AuthenticationScheme))
        {
            failures.Add("AuthenticationScheme must be specified.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Issuer must be specified.");
        }

        if (options.Audiences.Count == 0)
        {
            failures.Add("At least one Audience must be specified.");
        }
        else if (options.Audiences.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("Audiences cannot contain empty values.");
        }

        if (options.ClockSkew < TimeSpan.Zero || options.ClockSkew > TimeSpan.FromMinutes(15))
        {
            failures.Add("ClockSkew must be between 0 and 15 minutes.");
        }

        ValidateClaimKinds(options.ClaimKinds, failures);
        ValidateSigningKeys(options, environment, requireActiveSigningKey, failures);
        return failures;
    }

    private static void ValidateClaimKinds(
        IdentityJwtClaimKindOptions claimKinds,
        List<string> failures)
    {
        if (claimKinds.CacheDuration < TimeSpan.Zero)
        {
            failures.Add("ClaimKinds.CacheDuration cannot be negative.");
        }

        if (claimKinds.AccessTokenClaimKinds.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("ClaimKinds.AccessTokenClaimKinds cannot contain empty values.");
        }

        if (claimKinds.TransformationClaimKinds.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("ClaimKinds.TransformationClaimKinds cannot contain empty values.");
        }
    }

    private static void ValidateSigningKeys(
        IdentityJwtValidationOptions options,
        IHostEnvironment? environment,
        bool requireActiveSigningKey,
        List<string> failures)
    {
        if (options.SigningKeys.Count == 0)
        {
            failures.Add("At least one JwtSigningKeyDescriptor must be configured in SigningKeys.");
            return;
        }

        var seenKids = new HashSet<string>(StringComparer.Ordinal);
        var activeCount = 0;
        var isDevelopment = environment?.IsDevelopment() == true;

        foreach (var descriptor in options.SigningKeys)
        {
            if (string.IsNullOrWhiteSpace(descriptor.Kid))
            {
                failures.Add("Every SigningKey must specify a non-empty Kid.");
                continue;
            }

            if (!seenKids.Add(descriptor.Kid))
            {
                failures.Add($"Duplicate SigningKey Kid '{descriptor.Kid}' is not allowed.");
            }

            if (descriptor.IsActiveSigningKey)
            {
                activeCount++;
            }

            ValidateSingleKeyMaterial(descriptor, requireActiveSigningKey, isDevelopment, failures);
        }

        if (requireActiveSigningKey && activeCount == 0)
        {
            failures.Add("At least one SigningKey must have IsActiveSigningKey = true.");
        }
    }

    private static void ValidateSingleKeyMaterial(
        JwtSigningKeyDescriptor descriptor,
        bool requireActiveSigningKey,
        bool isDevelopment,
        List<string> failures)
    {
        if (!isDevelopment &&
            !string.IsNullOrWhiteSpace(descriptor.SymmetricKey) &&
            JwtKeyMaterialHelper.IsDemoKey(Encoding.UTF8.GetBytes(descriptor.SymmetricKey)))
        {
            failures.Add(
                "The public demo signing key is only allowed in Development. Configure a private signing key.");
            return;
        }

        try
        {
            var (_, owned) = JwtKeyMaterialHelper.CreateSecurityKey(
                descriptor,
                requirePrivateKey: requireActiveSigningKey && descriptor.IsActiveSigningKey);
            owned?.Dispose();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            failures.Add(ex.Message);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            failures.Add($"Invalid cryptographic key '{descriptor.Kid}': {ex.Message}");
        }
    }
}
