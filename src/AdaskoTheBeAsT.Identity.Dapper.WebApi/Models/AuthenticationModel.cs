using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

public class AuthenticationModel
{
    [JsonPropertyName("client_id")]
    [FromForm(Name = "client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    [FromForm(Name = "client_secret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("grant_type")]
    [FromForm(Name = "grant_type")]
    public string? GrantType { get; set; }

    public string? Password { get; set; }

    [JsonPropertyName("refresh_token")]
    [FromForm(Name = "refresh_token")]
    public string? RefreshToken { get; set; }

    public string? Username { get; set; }
}
