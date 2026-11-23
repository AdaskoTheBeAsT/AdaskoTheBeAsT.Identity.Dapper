namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public sealed record ClaimKindUpsertRequest(
    string Name,
    string? Description = null,
    bool IncludeInAccessToken = false,
    bool IncludeInTransformation = true,
    bool IsActive = true);
