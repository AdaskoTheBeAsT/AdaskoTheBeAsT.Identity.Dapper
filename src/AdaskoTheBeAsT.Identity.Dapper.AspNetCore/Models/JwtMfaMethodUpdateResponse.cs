using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtMfaMethodUpdateResponse(
    bool VerificationRequired,
    IReadOnlyList<string> EnabledMethods,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ChallengeToken = null);
