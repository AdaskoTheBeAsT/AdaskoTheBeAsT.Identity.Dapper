using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaAuthenticatorConfirmResponse(
    bool TwoFactorEnabled,
    IReadOnlyList<string> RecoveryCodes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JwtLoginResponse? LoginResponse = null);
