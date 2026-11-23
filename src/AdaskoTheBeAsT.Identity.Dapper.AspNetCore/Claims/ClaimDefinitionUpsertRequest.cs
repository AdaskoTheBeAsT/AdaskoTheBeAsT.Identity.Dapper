using System.Security.Claims;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public sealed record ClaimDefinitionUpsertRequest(
    string ClaimKindName,
    string ClaimType,
    string ValueType = ClaimValueTypes.String,
    string? DisplayName = null,
    string? Description = null,
    bool IsActive = true);
