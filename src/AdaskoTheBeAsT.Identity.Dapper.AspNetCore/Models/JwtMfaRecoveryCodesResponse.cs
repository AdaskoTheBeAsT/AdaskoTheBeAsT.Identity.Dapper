using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaRecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);
