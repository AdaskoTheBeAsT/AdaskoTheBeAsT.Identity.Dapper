namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

public enum MfaMethodType
{
    Authenticator = 0,
    Email = 1,
    Sms = 2,
    RecoveryCode = 3,
}
