namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaSetupLinkExchangeResponse(
    string MfaChallengeToken,
    int ChallengeExpiresIn,
    JwtMfaAuthenticatorSetupResponse AuthenticatorSetup);
