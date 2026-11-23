namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaVerifyRequest
{
    public string MfaChallengeToken { get; init; } = string.Empty;

    public string Method { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
