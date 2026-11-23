namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaResendRequest
{
    public string MfaChallengeToken { get; init; } = string.Empty;

    public string Method { get; init; } = string.Empty;
}
