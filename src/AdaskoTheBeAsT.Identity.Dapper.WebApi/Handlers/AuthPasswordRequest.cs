using System.Text.Json.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

[JsonDerivedType(typeof(AuthPasswordRequest), typeDiscriminator: "password")]
public class AuthPasswordRequest
    : AuthRequestBase
{
    public string? Username { get; set; }

    public string? Password { get; set; }
}
