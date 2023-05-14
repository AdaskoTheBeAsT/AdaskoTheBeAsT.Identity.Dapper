using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;

public class TokenService
    : ITokenService
{
    private const string Audience = "IdentityWebApi";
    private const string Bearer = "Bearer";
    private const string ClientIdClaim = "client_id";
    private const string Hyphen = "-";
    private const string RefreshTokenKeyTemplate = "RefreshToken_{0}";
    private const int TokenValidSeconds = 3600;
    private readonly TokenServiceOptions _options;
    private readonly IMemoryCache _memoryCache;

    public TokenService(
        IMemoryCache memoryCache,
        TokenServiceOptions options)
    {
        _memoryCache = memoryCache;
        _options = options;
    }

    public Token GenerateToken(
        ApplicationUser user,
        IList<string> roles,
        IList<Claim> claims)
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_options.SigningKey ?? string.Empty)),
            SecurityAlgorithms.HmacSha256);

        var claimsIdentity = new ClaimsIdentity();

        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty));
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty));
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty));
        claimsIdentity.AddClaim(new Claim(ClientIdClaim, user.Id.ToString("D")));
        claimsIdentity.AddClaims(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claimsIdentity.AddClaims(claims);

        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddSeconds(TokenValidSeconds);

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

        var refreshTokenId = Guid.NewGuid().ToString().Replace(Hyphen, string.Empty, StringComparison.OrdinalIgnoreCase);
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
            string.Format(CultureInfo.InvariantCulture, RefreshTokenKeyTemplate, refreshTokenId),
            refreshToken,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt,
            });

        return new Token
        {
            AccessToken = signedAndEncodedToken,
            TokenType = Bearer,
            ExpiresIn = TokenValidSeconds,
            RefreshToken = refreshTokenId,
            Audience = securityTokenDescriptor.Audience,
            UserName = user.UserName,
        };
    }

    public RefreshToken? GetRefreshToken(string refreshTokenId)
    {
        var found = _memoryCache.TryGetValue<RefreshToken>(
            string.Format(CultureInfo.InvariantCulture, RefreshTokenKeyTemplate, refreshTokenId),
            out var refreshToken);

        if (!found)
        {
            throw new InvalidRefreshTokenException();
        }

        return refreshToken;
    }
}
