using System.Text.Json.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

[JsonPolymorphic]
[JsonDerivedType(typeof(AuthClientCredentialRequest), typeDiscriminator: "client_credentials")]
[JsonDerivedType(typeof(AuthPasswordRequest), typeDiscriminator: "password")]
[JsonDerivedType(typeof(AuthRefreshTokenRequest), typeDiscriminator: "refresh_token")]
public abstract class AuthRequestBase;
