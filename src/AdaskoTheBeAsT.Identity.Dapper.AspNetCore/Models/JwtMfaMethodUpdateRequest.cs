namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaMethodUpdateRequest
{
    public string Method { get; init; } = string.Empty;

    public bool Enabled { get; init; }

    public string? ChallengeToken { get; init; }

    public string? VerificationCode { get; init; }
}
