namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

public enum MfaEnforcementMode
{
    PerUserSetting = 0,
    RequiredForAllUsers = 1,
    CustomPolicy = 2,
}
