using System;
using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

public sealed class IdentityJwtOptions : IdentityJwtValidationOptions
{
    public const string KnownDemoSigningKeySha256Hex = "CC421157721F40E6C9B336A0D5877525433DB0CE0C77EDDE050F4CD4A1B84E82";

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);

    public TimeSpan AbsoluteSessionLifetime { get; set; } = TimeSpan.FromDays(30);

    public string? TokenHashKey { get; set; }

    public string TokenHashKeyId { get; set; } = "default";

    public IDictionary<string, string> PreviousTokenHashKeys { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public bool AllowUnkeyedSha256InDevelopment { get; set; }

    public int InMemoryRefreshTokenCapacity { get; set; } = 10_000;

    public int InMemoryRevocationCapacity { get; set; } = 50_000;

    public bool UpdateSecurityStampOnRevokeAll { get; set; }

    public IdentityJwtMfaOptions Mfa { get; } = new();
}
