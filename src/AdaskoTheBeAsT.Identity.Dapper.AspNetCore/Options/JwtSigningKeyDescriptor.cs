namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

public sealed class JwtSigningKeyDescriptor
{
    public string Kid { get; set; } = "default";

    public JwtKeyAlgorithm Algorithm { get; set; } = JwtKeyAlgorithm.ES256;

    public string? Pem { get; set; }

    public string? SymmetricKey { get; set; }

    public bool IsActiveSigningKey { get; set; } = true;
}
