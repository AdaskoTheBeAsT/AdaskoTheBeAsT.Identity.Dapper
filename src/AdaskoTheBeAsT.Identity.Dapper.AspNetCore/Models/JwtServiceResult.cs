namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;

public sealed record JwtServiceResult<T>(
    JwtOperationStatus Status,
    T? Value = default,
    string? ErrorCode = null,
    string? ErrorDescription = null)
{
    public bool Succeeded => Status == JwtOperationStatus.Succeeded;

    public static JwtServiceResult<T> Success(T value) =>
        new(JwtOperationStatus.Succeeded, value);

    public static JwtServiceResult<T> Fail(
        JwtOperationStatus status,
        string errorCode,
        string? errorDescription = null) =>
        new(status, default, errorCode, errorDescription);
}
