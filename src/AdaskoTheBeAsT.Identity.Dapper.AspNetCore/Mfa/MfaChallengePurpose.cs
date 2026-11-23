namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public enum MfaChallengePurpose : byte
{
    LoginChallenge = 1,
    EnrollmentBootstrap = 2,
    SetupLink = 3,
    OtpReplayGuard = 4,
}
