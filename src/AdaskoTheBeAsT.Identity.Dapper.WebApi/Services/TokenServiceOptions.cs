namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;

public class TokenServiceOptions
{
    public string? SigningKey { get; set; }

    /// <summary>
    /// Gets or sets the positive maximum number of outstanding refresh tokens.
    /// When full, issuing a token evicts the oldest outstanding issuance.
    /// Expired and consumed tokens immediately release their slots.
    /// The value is snapshotted when the token service is constructed.
    /// </summary>
    public int RefreshTokenCapacity { get; set; } = 10000;
}
