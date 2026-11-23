namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaAuthenticatorSetupRequest
{
    public string? MfaChallengeToken { get; init; }
}
