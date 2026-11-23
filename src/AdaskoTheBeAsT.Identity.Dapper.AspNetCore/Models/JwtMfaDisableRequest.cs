namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaDisableRequest
{
    public string? Password { get; init; }

    public bool RevokeOtherSessions { get; init; } = true;
}
