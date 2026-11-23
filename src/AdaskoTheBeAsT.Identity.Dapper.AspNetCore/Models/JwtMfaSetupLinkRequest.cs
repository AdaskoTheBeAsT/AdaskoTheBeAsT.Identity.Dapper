namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaSetupLinkRequest
{
    public string? Channel { get; init; }
}
