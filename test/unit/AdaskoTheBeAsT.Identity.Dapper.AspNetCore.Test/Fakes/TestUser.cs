using System.Security.Claims;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;

internal sealed class TestUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public bool TwoFactorEnabled { get; set; }

    public bool LockoutEnabled { get; set; } = true;

    public DateTimeOffset? LockoutEnd { get; set; }

    public int AccessFailedCount { get; set; }

    public HashSet<string> Roles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<Claim> Claims { get; } = [];

    public Dictionary<(string Provider, string Name), string> Tokens { get; } = [];
}
