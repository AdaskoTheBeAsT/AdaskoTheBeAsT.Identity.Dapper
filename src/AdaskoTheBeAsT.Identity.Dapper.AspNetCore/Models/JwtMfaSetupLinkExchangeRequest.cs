namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaSetupLinkExchangeRequest
{
    public string SetupToken { get; init; } = string.Empty;
}
