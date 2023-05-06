using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;

public class TokenGenerationService
    : ITokenGenerationService
{
    private const string Audience = "IdentityWebApi";
    private readonly string _signingKey;
    private readonly IMemoryCache _memoryCache;

    public TokenGenerationService(
        string signingKey,
        IMemoryCache memoryCache)
    {
        _signingKey = signingKey;
        _memoryCache = memoryCache;
    }

    public Token GenerateToken(
        ApplicationUser user,
        IList<string> roles,
        IList<Claim> claims)
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_signingKey)),
            SecurityAlgorithms.HmacSha256);

        var claimsIdentity = new ClaimsIdentity();

        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty));
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty));
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty));
        claimsIdentity.AddClaim(new Claim("uid", user.Id.ToString("D")));
        claimsIdentity.AddClaims(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claimsIdentity.AddClaims(claims);

        var issuedAt = DateTimeOffset.UtcNow;
        var seconds = 3600;
        var expiresAt = issuedAt.AddSeconds(seconds);

        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = Audience,
            Issuer = Audience,
            Subject = claimsIdentity,
            SigningCredentials = signingCredentials,
            IssuedAt = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var plainToken = tokenHandler.CreateToken(securityTokenDescriptor);
        var signedAndEncodedToken = tokenHandler.WriteToken(plainToken);

        var refreshTokenId = Guid.NewGuid().ToString().Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
        var refreshToken = new RefreshToken
        {
            AudienceId = Audience,
            Subject = user.UserName,
            ExpiresUtc = plainToken.ValidTo,
            IssuedUtc = plainToken.ValidFrom,
            RefreshTokenId = refreshTokenId,
            ProtectedTicket = signedAndEncodedToken,
        };
        _memoryCache.Set(
            $"RefreshToken_{refreshTokenId}",
            refreshToken,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt,
            });

        return new Token
        {
            AccessToken = signedAndEncodedToken,
            TokenType = "bearer",
            ExpiresIn = seconds,
            RefreshToken = refreshTokenId,
            Audience = securityTokenDescriptor.Audience,
            UserName = user.UserName,
        };
    }
}
