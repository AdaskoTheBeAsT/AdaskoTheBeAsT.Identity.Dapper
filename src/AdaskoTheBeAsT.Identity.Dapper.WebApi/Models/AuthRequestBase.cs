using System.Text.Json.Serialization;
using MediatR;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "grant_type")]
[JsonDerivedType(typeof(AuthClientCredentialRequest), typeDiscriminator: "client_credentials")]
[JsonDerivedType(typeof(AuthPasswordRequest), typeDiscriminator: "password")]
[JsonDerivedType(typeof(AuthRefreshTokenRequest), typeDiscriminator: "refresh_token")]
public abstract class AuthRequestBase
    : IRequest<Token>
{
    // empty
}
