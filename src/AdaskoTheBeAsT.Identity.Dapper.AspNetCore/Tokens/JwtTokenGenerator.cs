using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private static readonly HashSet<string> ReservedClaimTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        IdentityJwtClaimTypes.Issuer,
        IdentityJwtClaimTypes.Audience,
        IdentityJwtClaimTypes.Subject,
        IdentityJwtClaimTypes.JwtId,
        IdentityJwtClaimTypes.SessionId,
        IdentityJwtClaimTypes.AuthenticationMethods,
        IdentityJwtClaimTypes.IssuedAt,
        IdentityJwtClaimTypes.NotBefore,
        IdentityJwtClaimTypes.Expiration,
        ClaimTypes.NameIdentifier,
    };

    private readonly IdentityJwtOptions _options;
    private readonly IJwtSigningCredentialsProvider _signingCredentialsProvider;
    private readonly TimeProvider _timeProvider;

    public JwtTokenGenerator(
        IOptions<IdentityJwtOptions> options,
        IJwtSigningCredentialsProvider signingCredentialsProvider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(signingCredentialsProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _options = options.Value;
        _signingCredentialsProvider = signingCredentialsProvider;
        _timeProvider = timeProvider;
    }

    public IssuedAccessToken GenerateAccessToken(JwtTokenDescriptorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.UserId))
        {
            throw new ArgumentException("Context UserId must be non-empty.", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.SessionId))
        {
            throw new ArgumentException("Context SessionId must be non-empty.", nameof(context));
        }

        var issuedAtUtc = _timeProvider.GetUtcNow();
        var expiresAtUtc = issuedAtUtc.Add(_options.AccessTokenLifetime);
        var jti = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

        var subjectClaims = context.CustomClaims
            .Where(claim => !ReservedClaimTypes.Contains(claim.Type))
            .ToList();

        subjectClaims.Add(new Claim(IdentityJwtClaimTypes.Subject, context.UserId));
        subjectClaims.Add(new Claim(ClaimTypes.NameIdentifier, context.UserId));
        subjectClaims.Add(new Claim(IdentityJwtClaimTypes.JwtId, jti));
        subjectClaims.Add(new Claim(IdentityJwtClaimTypes.SessionId, context.SessionId));

        var tokenClaims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [IdentityJwtClaimTypes.AuthenticationMethods] = context.AuthenticationMethods.Count > 0
                ? context.AuthenticationMethods.ToArray()
                : new[] { "pwd" },
        };

        if (_options.Audiences.Count > 1)
        {
            tokenClaims[IdentityJwtClaimTypes.Audience] = _options.Audiences.ToArray();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audiences.Count == 1 ? _options.Audiences[0] : null,
            Subject = new ClaimsIdentity(subjectClaims),
            Claims = tokenClaims,
            IssuedAt = issuedAtUtc.UtcDateTime,
            NotBefore = issuedAtUtc.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = _signingCredentialsProvider.GetSigningCredentials(),
        };

        var handler = new JsonWebTokenHandler();
        var encoded = handler.CreateToken(descriptor);
        var expiresInSeconds = (int)Math.Max(1, Math.Round(_options.AccessTokenLifetime.TotalSeconds));

        return new IssuedAccessToken(
            encoded,
            jti,
            context.SessionId,
            issuedAtUtc,
            expiresAtUtc,
            expiresInSeconds);
    }
}
