using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaStatusResponse(
    bool TwoFactorEnabled,
    bool HasAuthenticator,
    int RecoveryCodesLeft,
    IReadOnlyList<string> EnabledMethods);
