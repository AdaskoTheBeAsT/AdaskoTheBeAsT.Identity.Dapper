namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;

public sealed record RefreshTokenRotationResult(
    RefreshTokenRotationStatus Status,
    RefreshTokenRecord? ExistingRecord,
    RefreshTokenRecord? ReplacementRecord);
