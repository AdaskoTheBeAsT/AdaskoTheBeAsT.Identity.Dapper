namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;

public enum RefreshTokenRotationStatus
{
    Rotated = 0,
    NotFoundOrExpired = 1,
    Replayed = 2,
    Revoked = 3,
}
