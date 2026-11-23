namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaSetupLinkResponse(
    bool Sent,
    int ExpiresInSeconds);
