namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public sealed record ClaimDefinitionRecord(
    int Id,
    int ClaimKindId,
    string ClaimKindName,
    string ClaimType,
    string ValueType,
    string? DisplayName,
    string? Description,
    bool IncludeInAccessToken,
    bool IncludeInTransformation,
    bool IsActive);
