using System;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;

public sealed record RefreshTokenRecord(
    Guid Id,
    string TokenHash,
    string HashKeyId,
    string SessionId,
    string UserId,
    string? SecurityStamp,
    string AuthenticationMethods,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset SessionExpiresAtUtc,
    DateTimeOffset? ConsumedAtUtc = null,
    string? ReplacedByTokenHash = null,
    DateTimeOffset? RevokedAtUtc = null,
    string? RevocationReason = null);
