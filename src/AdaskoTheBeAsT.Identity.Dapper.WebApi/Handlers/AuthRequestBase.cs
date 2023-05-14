using System.Text.Json.Serialization;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using MediatR;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

[JsonPolymorphic]
[JsonDerivedType(typeof(AuthClientCredentialRequest), typeDiscriminator: "client_credentials")]
[JsonDerivedType(typeof(AuthPasswordRequest), typeDiscriminator: "password")]
[JsonDerivedType(typeof(AuthRefreshTokenRequest), typeDiscriminator: "refresh_token")]
public class AuthRequestBase
    : IRequest<Token>
{
}
