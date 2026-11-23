namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaAuthenticatorConfirmRequest
{
    public string Code { get; init; } = string.Empty;

    public string? MfaChallengeToken { get; init; }
}
