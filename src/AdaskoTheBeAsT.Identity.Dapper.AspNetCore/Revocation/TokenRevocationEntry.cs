using System;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;

public sealed record TokenRevocationEntry(
    string Issuer,
    RevocationSubjectType SubjectType,
    string SubjectKey,
    DateTimeOffset RevokedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedBeforeUtc = null,
    string? Reason = null);
