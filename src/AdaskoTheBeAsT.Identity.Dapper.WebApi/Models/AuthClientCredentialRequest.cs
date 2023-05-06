using System.Text.Json.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

public class AuthClientCredentialRequest
    : AuthRequestBase
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }
}
