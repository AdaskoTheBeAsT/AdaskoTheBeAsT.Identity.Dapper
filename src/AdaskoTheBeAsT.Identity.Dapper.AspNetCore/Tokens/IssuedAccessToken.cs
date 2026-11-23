using System;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;

public sealed record IssuedAccessToken(
    string AccessToken,
    string JwtId,
    string SessionId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    int ExpiresInSeconds);
