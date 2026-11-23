using System;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public sealed record MfaChallengeRecord(
    Guid ChallengeId,
    string TokenHash,
    string HashKeyId,
    MfaChallengePurpose Purpose,
    string UserId,
    string? SecurityStamp,
    string AllowedMethods,
    string? SelectedMethod,
    string? OtpCodeHash,
    int AttemptCount,
    int MaxAttempts,
    int ResendCount,
    DateTimeOffset? ResendAvailableAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);
