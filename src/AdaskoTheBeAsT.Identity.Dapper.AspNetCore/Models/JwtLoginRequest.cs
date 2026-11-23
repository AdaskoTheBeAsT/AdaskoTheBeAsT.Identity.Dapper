namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtLoginRequest
{
    public string? UserName { get; init; }

    public string? Email { get; init; }

    public string Password { get; init; } = string.Empty;
}
