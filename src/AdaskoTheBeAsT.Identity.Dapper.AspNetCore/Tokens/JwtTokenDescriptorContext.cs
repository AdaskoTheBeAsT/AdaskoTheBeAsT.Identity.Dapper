using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;

public sealed record JwtTokenDescriptorContext(
    string UserId,
    string SessionId,
    IReadOnlyList<string> AuthenticationMethods,
    IReadOnlyList<Claim> CustomClaims);
