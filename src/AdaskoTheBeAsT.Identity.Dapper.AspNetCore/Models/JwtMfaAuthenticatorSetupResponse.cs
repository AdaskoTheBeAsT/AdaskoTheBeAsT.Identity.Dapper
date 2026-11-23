using System.Text.Json.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaAuthenticatorSetupResponse(
    string SharedKey,
    string AuthenticatorUri,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? QrCodeSvg = null);
