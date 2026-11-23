namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public enum MfaChallengeAttemptStatus
{
    Allowed = 0,
    NotFoundOrExpired = 1,
    Exhausted = 2,
}
