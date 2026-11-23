using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using OtpNet;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

internal static class TotpCodeHelper
{
    private const int Digits = 6;
    private const int PeriodSeconds = 30;
    private const int SecretByteLength = 20;

    public static string GenerateSecretBase32()
    {
        var bytes = RandomNumberGenerator.GetBytes(SecretByteLength);
        try
        {
            return Base32Encoding.ToString(bytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public static string FormatKeyForManualEntry(string unformattedKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unformattedKey);
        var sb = new StringBuilder(unformattedKey.Length + (unformattedKey.Length / 4));
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            sb.Append(unformattedKey.AsSpan(currentPosition, 4));
            sb.Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
        {
            sb.Append(unformattedKey.AsSpan(currentPosition));
        }

        return sb.ToString().ToLowerInvariant();
    }

    public static string BuildOtpAuthUri(
        string issuer,
        string accountName,
        string unformattedBase32Secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentException.ThrowIfNullOrWhiteSpace(unformattedBase32Secret);

        var escapedIssuer = Uri.EscapeDataString(issuer);
        var escapedAccount = Uri.EscapeDataString(accountName);
        var escapedSecret = Uri.EscapeDataString(unformattedBase32Secret);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"otpauth://totp/{escapedIssuer}:{escapedAccount}?secret={escapedSecret}&issuer={escapedIssuer}&digits={Digits}&period={PeriodSeconds}");
    }

    public static bool TryValidateCode(
        string base32Secret,
        string candidateCode,
        DateTimeOffset nowUtc,
        out long matchedTimeStep)
    {
        matchedTimeStep = 0;
        var normalizedCode = NormalizeCode(candidateCode);
        if (normalizedCode.Length != Digits || !int.TryParse(normalizedCode, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        var normalizedKey = NormalizeCode(base32Secret.Trim()).TrimEnd('=');
        if (normalizedKey.Length < 2 || nowUtc < DateTimeOffset.UnixEpoch)
        {
            return false;
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Base32Encoding.ToBytes(normalizedKey);
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            return TryMatchTimeStep(keyBytes, normalizedCode, nowUtc, out matchedTimeStep);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    public static string NormalizeCode(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(rawCode.Length);
        foreach (var ch in rawCode)
        {
            if (ch is ' ' or '-')
            {
                continue;
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }

    private static bool TryMatchTimeStep(
        byte[] keyBytes,
        string normalizedCode,
        DateTimeOffset nowUtc,
        out long matchedTimeStep)
    {
        var totp = new Totp(keyBytes, PeriodSeconds, OtpHashMode.Sha1, Digits);
        var currentStep = nowUtc.ToUnixTimeSeconds() / PeriodSeconds;
        Span<byte> candidate = stackalloc byte[Digits];
        Span<byte> expected = stackalloc byte[Digits];
        _ = Encoding.ASCII.GetBytes(normalizedCode, candidate);

        // Keep oldest-first matching so existing replay markers still identify the same step.
        for (var step = Math.Max(0, currentStep - 1); step <= currentStep + 1; step++)
        {
            var timestamp = step * PeriodSeconds;
            if (timestamp > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            {
                break;
            }

            var code = totp.ComputeTotp(DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime);
            _ = Encoding.ASCII.GetBytes(code, expected);
            if (CryptographicOperations.FixedTimeEquals(expected, candidate))
            {
                matchedTimeStep = step;
                return true;
            }
        }

        matchedTimeStep = 0;
        return false;
    }
}
