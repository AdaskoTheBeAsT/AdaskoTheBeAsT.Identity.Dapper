using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

public class IdentityJwtValidationOptions
{
    public string AuthenticationScheme { get; set; } = JwtBearerDefaults.AuthenticationScheme;

    public string Issuer { get; set; } = string.Empty;

    public IList<string> Audiences { get; } = new List<string>();

    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);

    public RevocationFailureMode FailureMode { get; set; } = RevocationFailureMode.FailClosed;

    public bool MapInboundClaims { get; set; }

    public IList<JwtSigningKeyDescriptor> SigningKeys { get; } = new List<JwtSigningKeyDescriptor>();

    public IdentityJwtClaimKindOptions ClaimKinds { get; } = new();
}
