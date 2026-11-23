namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public sealed record ResolvedClaimRecord(
    string ClaimType,
    string ClaimValue,
    string ValueType,
    string ClaimKindName);
