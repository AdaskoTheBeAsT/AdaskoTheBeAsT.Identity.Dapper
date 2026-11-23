namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;

public enum RevocationSubjectType : byte
{
    Jti = 1,
    SessionId = 2,
    UserId = 3,
}
