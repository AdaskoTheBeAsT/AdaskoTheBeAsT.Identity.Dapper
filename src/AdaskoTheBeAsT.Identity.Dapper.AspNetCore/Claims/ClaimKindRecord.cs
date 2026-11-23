namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public sealed record ClaimKindRecord(
    int Id,
    string Name,
    string? Description,
    bool IncludeInAccessToken,
    bool IncludeInTransformation,
    bool IsActive);
