using System;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;

internal static class LegacyTokenMapper
{
    public static Token Map(JwtLoginResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        string? audience = null;
        string? userName = null;
        if (!string.IsNullOrWhiteSpace(response.AccessToken))
        {
            var handler = new JsonWebTokenHandler();
            if (handler.CanReadToken(response.AccessToken))
            {
                var jwt = handler.ReadJsonWebToken(response.AccessToken);
                audience = jwt.Audiences.FirstOrDefault();
                if (jwt.TryGetPayloadValue<string>(IdentityJwtClaimTypes.UniqueName, out var parsedUserName))
                {
                    userName = parsedUserName;
                }
            }
        }

        return new Token
        {
            AccessToken = response.AccessToken,
            TokenType = response.TokenType ?? "Bearer",
            ExpiresIn = response.ExpiresIn,
            RefreshToken = response.RefreshToken,
            Audience = audience,
            UserName = userName,
        };
    }
}
