using System.Text.Json.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

public class AuthRefreshTokenRequest
    : AuthRequestBase
{
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}
