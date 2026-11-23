namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public sealed record MfaChallengeAttemptResult(
    MfaChallengeAttemptStatus Status,
    MfaChallengeRecord? Record);
