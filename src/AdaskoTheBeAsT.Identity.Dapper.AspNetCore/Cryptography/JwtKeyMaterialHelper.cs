using System;
using System.Security.Cryptography;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.IdentityModel.Tokens;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;

internal static class JwtKeyMaterialHelper
{
    public static string MapSecurityAlgorithm(JwtKeyAlgorithm algorithm) =>
        algorithm switch
        {
            JwtKeyAlgorithm.ES256 => SecurityAlgorithms.EcdsaSha256,
            JwtKeyAlgorithm.ES384 => SecurityAlgorithms.EcdsaSha384,
            JwtKeyAlgorithm.PS256 => SecurityAlgorithms.RsaSsaPssSha256,
            JwtKeyAlgorithm.PS384 => SecurityAlgorithms.RsaSsaPssSha384,
            JwtKeyAlgorithm.RS256 => SecurityAlgorithms.RsaSha256,
            JwtKeyAlgorithm.HS256 => SecurityAlgorithms.HmacSha256,
            JwtKeyAlgorithm.HS384 => SecurityAlgorithms.HmacSha384,
            JwtKeyAlgorithm.HS512 => SecurityAlgorithms.HmacSha512,
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported JWT algorithm."),
        };

    public static bool IsDemoKey(ReadOnlySpan<byte> keyBytes)
    {
        var hashHex = Convert.ToHexString(SHA256.HashData(keyBytes));
        return string.Equals(hashHex, IdentityJwtOptions.KnownDemoSigningKeySha256Hex, StringComparison.Ordinal);
    }

    public static (SecurityKey Key, IDisposable? OwnedResource) CreateSecurityKey(
        JwtSigningKeyDescriptor descriptor,
        bool requirePrivateKey)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Algorithm switch
        {
            JwtKeyAlgorithm.HS256 => CreateSymmetricKey(descriptor, minBytes: 32),
            JwtKeyAlgorithm.HS384 => CreateSymmetricKey(descriptor, minBytes: 48),
            JwtKeyAlgorithm.HS512 => CreateSymmetricKey(descriptor, minBytes: 64),
            JwtKeyAlgorithm.ES256 => CreateEcdsaKey(descriptor, expectedKeySizeBits: 256, requirePrivateKey),
            JwtKeyAlgorithm.ES384 => CreateEcdsaKey(descriptor, expectedKeySizeBits: 384, requirePrivateKey),
            JwtKeyAlgorithm.PS256 or JwtKeyAlgorithm.PS384 or JwtKeyAlgorithm.RS256 =>
                CreateRsaKey(descriptor, requirePrivateKey),
            _ => throw new InvalidOperationException($"Unsupported JWT key algorithm '{descriptor.Algorithm}'."),
        };
    }

    private static (SecurityKey Key, IDisposable? OwnedResource) CreateSymmetricKey(
        JwtSigningKeyDescriptor descriptor,
        int minBytes)
    {
        if (string.IsNullOrWhiteSpace(descriptor.SymmetricKey))
        {
            throw new InvalidOperationException(
                $"JWT key '{descriptor.Kid}' using '{descriptor.Algorithm}' requires SymmetricKey.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(descriptor.SymmetricKey);
        if (keyBytes.Length < minBytes)
        {
            throw new InvalidOperationException(
                $"JWT key '{descriptor.Kid}' using '{descriptor.Algorithm}' requires at least {minBytes} UTF-8 bytes.");
        }

        var key = new SymmetricSecurityKey(keyBytes) { KeyId = descriptor.Kid };
        return (key, null);
    }

    private static (SecurityKey Key, IDisposable? OwnedResource) CreateEcdsaKey(
        JwtSigningKeyDescriptor descriptor,
        int expectedKeySizeBits,
        bool requirePrivateKey)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Pem))
        {
            throw new InvalidOperationException(
                $"JWT key '{descriptor.Kid}' using '{descriptor.Algorithm}' requires Pem.");
        }

        var ecdsa = ECDsa.Create();
        try
        {
            ecdsa.ImportFromPem(descriptor.Pem);
            if (ecdsa.KeySize != expectedKeySizeBits)
            {
                throw new InvalidOperationException(
                    $"JWT key '{descriptor.Kid}' using '{descriptor.Algorithm}' requires a {expectedKeySizeBits}-bit EC curve.");
            }

            if (requirePrivateKey)
            {
                var parameters = ecdsa.ExportParameters(includePrivateParameters: true);
                if (parameters.D is not { Length: > 0 })
                {
                    throw new InvalidOperationException(
                        $"Active signing key '{descriptor.Kid}' must contain private EC key parameters.");
                }
            }

            var securityKey = new ECDsaSecurityKey(ecdsa) { KeyId = descriptor.Kid };
            return (securityKey, ecdsa);
        }
        catch
        {
            ecdsa.Dispose();
            throw;
        }
    }

    private static (SecurityKey Key, IDisposable? OwnedResource) CreateRsaKey(
        JwtSigningKeyDescriptor descriptor,
        bool requirePrivateKey)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Pem))
        {
            throw new InvalidOperationException(
                $"JWT key '{descriptor.Kid}' using '{descriptor.Algorithm}' requires Pem.");
        }

        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(descriptor.Pem);
            if (rsa.KeySize < 2048)
            {
                throw new InvalidOperationException(
                    $"JWT key '{descriptor.Kid}' using '{descriptor.Algorithm}' requires at least a 2048-bit RSA key.");
            }

            if (requirePrivateKey)
            {
                var parameters = rsa.ExportParameters(includePrivateParameters: true);
                if (parameters.D is not { Length: > 0 })
                {
                    throw new InvalidOperationException(
                        $"Active signing key '{descriptor.Kid}' must contain private RSA key parameters.");
                }
            }

            var securityKey = new RsaSecurityKey(rsa) { KeyId = descriptor.Kid };
            return (securityKey, rsa);
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }
}
