namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public static class IdentityJwtClaimTypes
{
    public const string Issuer = "iss";
    public const string Audience = "aud";
    public const string Subject = "sub";
    public const string JwtId = "jti";
    public const string SessionId = "sid";
    public const string AuthenticationMethods = "amr";
    public const string MappedAuthenticationMethods = "http://schemas.microsoft.com/claims/authnmethodsreferences";
    public const string IssuedAt = "iat";
    public const string NotBefore = "nbf";
    public const string Expiration = "exp";
    public const string UniqueName = "unique_name";
    public const string Email = "email";
    public const string ClaimsTransformed = "urn:adaskothebeast:identity:claims_transformed";
}
