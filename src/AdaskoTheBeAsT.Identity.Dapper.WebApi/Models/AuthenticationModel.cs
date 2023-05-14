using System.Text.Json.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

public class AuthenticationModel
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("grant_type")]
    public string? GrantType { get; set; }

    public string? Password { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    public string? Username { get; set; }
}
