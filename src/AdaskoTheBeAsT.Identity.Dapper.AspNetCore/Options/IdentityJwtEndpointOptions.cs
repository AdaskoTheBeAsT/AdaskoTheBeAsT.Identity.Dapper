namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

public sealed class IdentityJwtEndpointOptions
{
    public bool EnableRegistration { get; set; }

    public bool EnableMfaEndpoints { get; set; } = true;

    public bool EnableMfaSetupLinkEndpoints { get; set; }
}
