namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtRefreshRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
