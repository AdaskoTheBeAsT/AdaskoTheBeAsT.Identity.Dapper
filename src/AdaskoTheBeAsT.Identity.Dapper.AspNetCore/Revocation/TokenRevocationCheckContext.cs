using System;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;

public sealed record TokenRevocationCheckContext(
    string Issuer,
    string? JwtId,
    string? SessionId,
    string? UserId,
    DateTimeOffset? IssuedAtUtc);
