using System.Text.Json.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class AuthRefreshTokenRequest
    : AuthRequestBase
{
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}
