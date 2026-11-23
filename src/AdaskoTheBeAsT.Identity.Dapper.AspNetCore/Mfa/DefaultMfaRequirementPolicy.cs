using System;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public sealed class DefaultMfaRequirementPolicy<TUser> : IMfaRequirementPolicy<TUser>
    where TUser : class
{
    private readonly UserManager<TUser> _userManager;
    private readonly IOptionsMonitor<IdentityJwtOptions> _optionsMonitor;

    public DefaultMfaRequirementPolicy(
        UserManager<TUser> userManager,
        IOptionsMonitor<IdentityJwtOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        _userManager = userManager;
        _optionsMonitor = optionsMonitor;
    }

    public Task<bool> IsMfaRequiredAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        var mode = _optionsMonitor.CurrentValue.Mfa.EnforcementMode;
        if (mode == MfaEnforcementMode.RequiredForAllUsers)
        {
            return Task.FromResult(true);
        }

        return _userManager.SupportsUserTwoFactor
            ? _userManager.GetTwoFactorEnabledAsync(user)
            : Task.FromResult(false);
    }
}
