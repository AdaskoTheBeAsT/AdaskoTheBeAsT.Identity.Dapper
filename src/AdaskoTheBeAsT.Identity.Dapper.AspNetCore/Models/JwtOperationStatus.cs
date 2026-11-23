namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public enum JwtOperationStatus
{
    Succeeded = 0,
    InvalidRequest = 1,
    Unauthorized = 2,
    Forbidden = 3,
    LockedOut = 4,
    TooManyRequests = 5,
    Conflict = 6,
}
