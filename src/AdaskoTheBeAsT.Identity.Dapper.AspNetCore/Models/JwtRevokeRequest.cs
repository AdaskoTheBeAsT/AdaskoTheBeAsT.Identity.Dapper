namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtRevokeRequest
{
    public string? Scope { get; init; }

    public bool UpdateSecurityStamp { get; init; }
}
