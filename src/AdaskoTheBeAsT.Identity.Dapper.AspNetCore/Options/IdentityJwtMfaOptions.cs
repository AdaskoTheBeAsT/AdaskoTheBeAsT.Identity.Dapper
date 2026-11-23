using System;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

public sealed class IdentityJwtMfaOptions
{
    public MfaEnforcementMode EnforcementMode { get; set; } = MfaEnforcementMode.PerUserSetting;

    public string AuthenticatorIssuer { get; set; } = "IdentityApi";

    public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromMinutes(5);

    public int MaxChallengeAttempts { get; set; } = 5;

    public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromSeconds(30);

    public int MaxResendCount { get; set; } = 3;

    public TimeSpan OtpReplayWindow { get; set; } = TimeSpan.FromSeconds(90);

    public TimeSpan SetupLinkLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public Uri? PublicSetupBaseUri { get; set; }

    public bool ProtectAuthenticatorKeyAtRest { get; set; } = true;

    public int RecoveryCodeCount { get; set; } = 10;

    public bool AllowRecoveryCodes { get; set; } = true;

    public bool AllowEmailMfa { get; set; } = true;

    public bool AllowSmsMfa { get; set; }

    public int InMemoryChallengeCapacity { get; set; } = 10_000;
}
