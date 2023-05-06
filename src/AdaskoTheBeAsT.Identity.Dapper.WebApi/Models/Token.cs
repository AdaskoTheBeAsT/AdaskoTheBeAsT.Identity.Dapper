using System.Text.Json.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

public class Token
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("audience")]
    public string? Audience { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
