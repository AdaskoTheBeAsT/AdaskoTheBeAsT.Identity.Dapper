using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;

public sealed class HmacTokenSecretHasher : ITokenSecretHasher
{
    private const string UnkeyedSha256KeyId = "__unkeyed_sha256__";

    private readonly string _activeKeyId;
    private readonly byte[]? _activeKeyBytes;
    private readonly Dictionary<string, byte[]> _keysById;
    private readonly bool _allowUnkeyedFallback;

    public HmacTokenSecretHasher(IOptions<IdentityJwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value;
        _activeKeyId = string.IsNullOrWhiteSpace(value.TokenHashKeyId) ? "default" : value.TokenHashKeyId;
        _allowUnkeyedFallback = value.AllowUnkeyedSha256InDevelopment;
        _keysById = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(value.TokenHashKey))
        {
            _activeKeyBytes = Encoding.UTF8.GetBytes(value.TokenHashKey);
            _keysById[_activeKeyId] = _activeKeyBytes;
        }

        foreach (var pair in value.PreviousTokenHashKeys.Where(
                     pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value)))
        {
            _keysById[pair.Key] = Encoding.UTF8.GetBytes(pair.Value);
        }
    }

    public string GenerateOpaqueToken(int byteCount = 32)
    {
        if (byteCount < 16)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount), "Opaque tokens require at least 16 random bytes.");
        }

        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Base64Url.EncodeToString(bytes);
    }

    public string GenerateSixDigitCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6", CultureInfo.InvariantCulture);
    }

    public HashedSecret HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        var tokenBytes = Encoding.UTF8.GetBytes(rawToken);
        if (_activeKeyBytes is { Length: >= 32 })
        {
            var digest = HMACSHA256.HashData(_activeKeyBytes, tokenBytes);
            return new HashedSecret(Convert.ToHexString(digest), _activeKeyId);
        }

        if (_allowUnkeyedFallback)
        {
            var digest = SHA256.HashData(tokenBytes);
            return new HashedSecret(Convert.ToHexString(digest), UnkeyedSha256KeyId);
        }

        throw new InvalidOperationException("IdentityJwtOptions.TokenHashKey must contain at least 32 UTF-8 bytes.");
    }

    public IReadOnlyList<HashedSecret> ComputeCandidateTokenHashes(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return [];
        }

        var tokenBytes = Encoding.UTF8.GetBytes(rawToken);
        var list = new List<HashedSecret>(_keysById.Count + 1);

        if (_activeKeyBytes is { Length: >= 32 })
        {
            var activeDigest = HMACSHA256.HashData(_activeKeyBytes, tokenBytes);
            list.Add(new HashedSecret(Convert.ToHexString(activeDigest), _activeKeyId));
        }

        foreach (var pair in _keysById.Where(pair => !string.Equals(pair.Key, _activeKeyId, StringComparison.Ordinal)))
        {
            var prevDigest = HMACSHA256.HashData(pair.Value, tokenBytes);
            list.Add(new HashedSecret(Convert.ToHexString(prevDigest), pair.Key));
        }

        if (_allowUnkeyedFallback)
        {
            var shaDigest = SHA256.HashData(tokenBytes);
            list.Add(new HashedSecret(Convert.ToHexString(shaDigest), UnkeyedSha256KeyId));
        }

        return list;
    }

    public bool VerifyToken(
        string rawToken,
        string expectedHashHex,
        string keyId)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(expectedHashHex))
        {
            return false;
        }

        return VerifyPayload(Encoding.UTF8.GetBytes(rawToken), expectedHashHex, keyId, allowUnkeyed: _allowUnkeyedFallback);
    }

    public HashedSecret HashScopedCode(
        string scope,
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (_activeKeyBytes is not { Length: >= 32 })
        {
            throw new InvalidOperationException(
                "IdentityJwtOptions.TokenHashKey (>= 32 UTF-8 bytes) is required to hash short OTP codes.");
        }

        var payload = BuildLengthPrefixedPayload(scope, code);
        var digest = HMACSHA256.HashData(_activeKeyBytes, payload);
        return new HashedSecret(Convert.ToHexString(digest), _activeKeyId);
    }

    public bool VerifyScopedCode(
        string scope,
        string code,
        string expectedHashHex,
        string keyId)
    {
        if (string.IsNullOrWhiteSpace(scope) ||
            string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(expectedHashHex))
        {
            return false;
        }

        var payload = BuildLengthPrefixedPayload(scope, code);
        return VerifyPayload(payload, expectedHashHex, keyId, allowUnkeyed: false);
    }

    private static byte[] BuildLengthPrefixedPayload(
        string scope,
        string code)
    {
        var formatted = string.Create(
            CultureInfo.InvariantCulture,
            $"{scope.Length}:{scope}|{code.Length}:{code}");
        return Encoding.UTF8.GetBytes(formatted);
    }

    private bool VerifyPayload(
        byte[] payload,
        string expectedHashHex,
        string keyId,
        bool allowUnkeyed)
    {
        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHashHex);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] candidateBytes;
        if (allowUnkeyed && string.Equals(keyId, UnkeyedSha256KeyId, StringComparison.Ordinal))
        {
            candidateBytes = SHA256.HashData(payload);
        }
        else if (_keysById.TryGetValue(keyId, out var keyBytes))
        {
            candidateBytes = HMACSHA256.HashData(keyBytes, payload);
        }
        else
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(candidateBytes, expectedBytes);
    }
}
