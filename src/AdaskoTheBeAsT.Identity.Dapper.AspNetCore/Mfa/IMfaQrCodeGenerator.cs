namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public interface IMfaQrCodeGenerator
{
    string GenerateSvg(string otpAuthUri);
}
