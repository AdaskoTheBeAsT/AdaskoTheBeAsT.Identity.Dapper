using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;

public sealed class TokenService
    : ITokenService, IDisposable
{
    private const string Audience = "IdentityWebApi";
    private const string Bearer = "Bearer";
    private const string ClientIdClaim = "client_id";
    private const int TokenValidSeconds = 3600;
    private readonly string _signingKey;
    private readonly int _refreshTokenCapacity;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, LinkedListNode<RefreshTokenEntry>> _refreshTokens = new(StringComparer.Ordinal);
    private readonly LinkedList<RefreshTokenEntry> _issuanceOrder = new();
    private readonly SortedSet<LinkedListNode<RefreshTokenEntry>> _expirationOrder =
        new(Comparer<LinkedListNode<RefreshTokenEntry>>.Create(CompareExpiration));
    private readonly object _refreshTokenLock = new();
    private bool _disposed;

    public TokenService(
        TokenServiceOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.RefreshTokenCapacity);

        // Snapshot configuration so later mutations cannot invalidate the store's bound.
        _signingKey = options.SigningKey ?? string.Empty;
        _refreshTokenCapacity = options.RefreshTokenCapacity;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the number of outstanding, unexpired refresh tokens in this service's isolated store.
    /// </summary>
    public int OutstandingRefreshTokenCount
    {
        get
        {
            lock (_refreshTokenLock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                RemoveExpiredRefreshTokens(_timeProvider.GetUtcNow().UtcDateTime);
                return _refreshTokens.Count;
            }
        }
    }

    public Token GenerateToken(
        ApplicationUser user,
        IList<string> roles,
        IList<Claim> claims)
    {
        lock (_refreshTokenLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
            SecurityAlgorithms.HmacSha256);

        var claimsIdentity = new ClaimsIdentity();

        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty));
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString("D")));
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty));
        claimsIdentity.AddClaim(new Claim(ClientIdClaim, user.Id.ToString("D")));
        claimsIdentity.AddClaims(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claimsIdentity.AddClaims(claims);

        var issuedAt = _timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddSeconds(TokenValidSeconds);

        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = Audience,
            Issuer = Audience,
            Subject = claimsIdentity,
            SigningCredentials = signingCredentials,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var plainToken = tokenHandler.CreateToken(securityTokenDescriptor);
        var signedAndEncodedToken = tokenHandler.WriteToken(plainToken);

        var refreshTokenId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var refreshToken = new RefreshToken
        {
            AudienceId = Audience,
            Subject = user.Id.ToString("D"),
            SecurityStamp = user.SecurityStamp,
            ExpiresUtc = plainToken.ValidTo,
        };
        lock (_refreshTokenLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            RemoveExpiredRefreshTokens(now);
            if (refreshToken.ExpiresUtc <= now)
            {
                throw new InvalidOperationException("The token expired before it could be issued.");
            }

            while (_refreshTokens.ContainsKey(refreshTokenId))
            {
                refreshTokenId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            }

            // Evict the oldest outstanding issuance before admitting the new token.
            // No shared IMemoryCache entries or asynchronous eviction callbacks are involved.
            if (_refreshTokens.Count == _refreshTokenCapacity)
            {
                RemoveRefreshToken(_issuanceOrder.First!);
            }

            var node = _issuanceOrder.AddLast(new RefreshTokenEntry(refreshTokenId, refreshToken));
            _refreshTokens.Add(refreshTokenId, node);
            _expirationOrder.Add(node);
        }

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

    public RefreshToken ConsumeRefreshToken(string refreshTokenId)
    {
        lock (_refreshTokenLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            RemoveExpiredRefreshTokens(_timeProvider.GetUtcNow().UtcDateTime);
            if (!_refreshTokens.TryGetValue(refreshTokenId, out var node))
            {
                throw new InvalidRefreshTokenException();
            }

            RemoveRefreshToken(node);
            return node.Value.Token;
        }
    }

    /// <summary>
    /// Clears outstanding tokens when the owning singleton container is disposed.
    /// The externally supplied clock is not owned or disposed by this service.
    /// </summary>
    public void Dispose()
    {
        lock (_refreshTokenLock)
        {
            _disposed = true;
            _refreshTokens.Clear();
            _issuanceOrder.Clear();
            _expirationOrder.Clear();
        }

        GC.SuppressFinalize(this);
    }

    private static int CompareExpiration(
        LinkedListNode<RefreshTokenEntry> left,
        LinkedListNode<RefreshTokenEntry> right)
    {
        var comparison = left.Value.Token.ExpiresUtc.CompareTo(right.Value.Token.ExpiresUtc);
        return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left.Value.Id, right.Value.Id);
    }

    private void RemoveExpiredRefreshTokens(DateTime now)
    {
        while (_expirationOrder.Min is { } node && node.Value.Token.ExpiresUtc <= now)
        {
            RemoveRefreshToken(node);
        }
    }

    private void RemoveRefreshToken(LinkedListNode<RefreshTokenEntry> node)
    {
        _refreshTokens.Remove(node.Value.Id);
        _issuanceOrder.Remove(node);
        _expirationOrder.Remove(node);
    }

    private sealed record RefreshTokenEntry(string Id, RefreshToken Token);
}
