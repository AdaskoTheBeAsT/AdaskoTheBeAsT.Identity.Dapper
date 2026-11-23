using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

internal sealed class MfaUserTokenManager<TUser>
    where TUser : class
{
    internal const string IdentityStoreLoginProvider = "[AspNetUserStore]";
    internal const string AuthenticatorKeyTokenName = "AuthenticatorKey";
    internal const string RecoveryCodesTokenName = "RecoveryCodes";
    internal const string MfaLoginProvider = "[IdentityJwtMfa]";
    internal const string PendingAuthenticatorKeyTokenName = "PendingAuthenticatorKey";
    internal const string EnabledMethodsTokenName = "EnabledMethods";
    internal const string ProtectedPrefix = "dpv1:";
    internal const string DataProtectionPurpose = "AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa.AuthenticatorKey.v1";

    private readonly UserManager<TUser> _userManager;
    private readonly IOptionsMonitor<IdentityJwtOptions> _optionsMonitor;
    private readonly IDataProtector? _protector;

    public MfaUserTokenManager(
        UserManager<TUser> userManager,
        IOptionsMonitor<IdentityJwtOptions> optionsMonitor,
        IDataProtectionProvider? dataProtectionProvider = null)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        _userManager = userManager;
        _optionsMonitor = optionsMonitor;
        _protector = dataProtectionProvider?.CreateProtector(DataProtectionPurpose);
    }

    public async Task<string?> GetActiveAuthenticatorKeyAsync(TUser user)
    {
        var raw = await _userManager.GetAuthenticationTokenAsync(
                user,
                IdentityStoreLoginProvider,
                AuthenticatorKeyTokenName)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = await _userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        }

        return UnprotectSecretIfNeeded(raw);
    }

    public async Task SetActiveAuthenticatorKeyAsync(
        TUser user,
        string unformattedBase32Secret)
    {
        var stored = ProtectSecretIfConfigured(unformattedBase32Secret);
        var result = await _userManager.SetAuthenticationTokenAsync(
                user,
                IdentityStoreLoginProvider,
                AuthenticatorKeyTokenName,
                stored)
            .ConfigureAwait(false);
        EnsureSucceeded(result, "Failed to store active authenticator key.");
    }

    public async Task<string?> GetPendingAuthenticatorKeyAsync(TUser user)
    {
        var raw = await _userManager.GetAuthenticationTokenAsync(
                user,
                MfaLoginProvider,
                PendingAuthenticatorKeyTokenName)
            .ConfigureAwait(false);

        return UnprotectSecretIfNeeded(raw);
    }

    public async Task SetPendingAuthenticatorKeyAsync(
        TUser user,
        string unformattedBase32Secret)
    {
        var stored = ProtectSecretIfConfigured(unformattedBase32Secret);
        var result = await _userManager.SetAuthenticationTokenAsync(
                user,
                MfaLoginProvider,
                PendingAuthenticatorKeyTokenName,
                stored)
            .ConfigureAwait(false);
        EnsureSucceeded(result, "Failed to store pending authenticator key.");
    }

    public Task RemovePendingAuthenticatorKeyAsync(TUser user) =>
        _userManager.RemoveAuthenticationTokenAsync(
            user,
            MfaLoginProvider,
            PendingAuthenticatorKeyTokenName);

    public async Task<IReadOnlyList<MfaMethodType>> GetEnabledMethodsAsync(TUser user)
    {
        var methods = new List<MfaMethodType>();
        var raw = await _userManager.GetAuthenticationTokenAsync(
                user,
                MfaLoginProvider,
                EnabledMethodsTokenName)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(raw))
        {
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse<MfaMethodType>(part, ignoreCase: true, out var parsed) &&
                    parsed != MfaMethodType.RecoveryCode &&
                    !methods.Contains(parsed))
                {
                    methods.Add(parsed);
                }
            }
        }

        var activeKey = await GetActiveAuthenticatorKeyAsync(user).ConfigureAwait(false);
        var twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(activeKey) && twoFactorEnabled && !methods.Contains(MfaMethodType.Authenticator))
        {
            methods.Insert(0, MfaMethodType.Authenticator);
        }

        return methods;
    }

    public async Task SetEnabledMethodsAsync(
        TUser user,
        IEnumerable<MfaMethodType> methods)
    {
        var normalized = methods
            .Where(m => m != MfaMethodType.RecoveryCode)
            .Distinct()
            .Select(m => m.ToString())
            .ToArray();

        if (normalized.Length == 0)
        {
            await _userManager.RemoveAuthenticationTokenAsync(
                    user,
                    MfaLoginProvider,
                    EnabledMethodsTokenName)
                .ConfigureAwait(false);
            return;
        }

        var joined = string.Join(',', normalized);
        var result = await _userManager.SetAuthenticationTokenAsync(
                user,
                MfaLoginProvider,
                EnabledMethodsTokenName,
                joined)
            .ConfigureAwait(false);
        EnsureSucceeded(result, "Failed to update enabled MFA methods.");
    }

    public async Task ClearAllMfaStateAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _userManager.SetTwoFactorEnabledAsync(user, enabled: false).ConfigureAwait(false);
        await _userManager.RemoveAuthenticationTokenAsync(
                user,
                IdentityStoreLoginProvider,
                AuthenticatorKeyTokenName)
            .ConfigureAwait(false);
        await _userManager.RemoveAuthenticationTokenAsync(
                user,
                IdentityStoreLoginProvider,
                RecoveryCodesTokenName)
            .ConfigureAwait(false);
        await _userManager.RemoveAuthenticationTokenAsync(
                user,
                MfaLoginProvider,
                PendingAuthenticatorKeyTokenName)
            .ConfigureAwait(false);
        await _userManager.RemoveAuthenticationTokenAsync(
                user,
                MfaLoginProvider,
                EnabledMethodsTokenName)
            .ConfigureAwait(false);
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string fallbackMessage)
    {
        if (!result.Succeeded)
        {
            var detail = result.Errors.FirstOrDefault()?.Description ?? fallbackMessage;
            throw new InvalidOperationException(detail);
        }
    }

    private string ProtectSecretIfConfigured(string plaintext)
    {
        if (_optionsMonitor.CurrentValue.Mfa.ProtectAuthenticatorKeyAtRest && _protector != null)
        {
            return ProtectedPrefix + _protector.Protect(plaintext);
        }

        return plaintext;
    }

    private string? UnprotectSecretIfNeeded(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        if (!stored.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
        {
            return stored;
        }

        if (_protector == null)
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(stored[ProtectedPrefix.Length..]);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
