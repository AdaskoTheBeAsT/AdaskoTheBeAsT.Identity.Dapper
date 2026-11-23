using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;

internal static class Rfc6238TestHelper
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "RFC 6238 TOTP specifies HMAC-SHA1.")]
    public static string ComputeTotp(string base32Secret, DateTimeOffset timestampUtc)
    {
        var key = DecodeBase32(base32Secret);
        var counter = timestampUtc.ToUnixTimeSeconds() / 30L;
        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);

        Span<byte> hmac = stackalloc byte[HMACSHA1.HashSizeInBytes];
        HMACSHA1.HashData(key, counterBytes, hmac);

        var offset = hmac[^1] & 0x0F;
        var binary = ((hmac[offset] & 0x7F) << 24)
                     | (hmac[offset + 1] << 16)
                     | (hmac[offset + 2] << 8)
                     | hmac[offset + 3];
        var otp = binary % 1_000_000;
        return otp.ToString("D6", CultureInfo.InvariantCulture);
    }

    private static byte[] DecodeBase32(string raw)
    {
        var cleaned = raw.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .TrimEnd('=')
            .ToUpperInvariant();

        var output = new List<byte>((cleaned.Length * 5) / 8);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var ch in cleaned)
        {
            var idx = Alphabet.IndexOf(ch, StringComparison.Ordinal);
            if (idx < 0)
            {
                throw new FormatException($"Invalid Base32 char '{ch}'.");
            }

            buffer = (buffer << 5) | idx;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return output.ToArray();
    }
}
