using System;
using QRCoder;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

internal sealed class QrCoderMfaQrCodeGenerator : IMfaQrCodeGenerator
{
    public string GenerateSvg(string otpAuthUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(otpAuthUri);

        return SvgQRCodeHelper.GetQRCode(
            otpAuthUri,
            pixelsPerModule: 1,
            darkColorHex: "#000000",
            lightColorHex: "#ffffff",
            QRCodeGenerator.ECCLevel.M,
            sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);
    }
}
