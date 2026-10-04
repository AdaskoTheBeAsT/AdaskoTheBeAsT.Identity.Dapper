namespace AdaskoTheBeAsT.Identity.Dapper;

internal static class IdentityStoreParameters
{
    internal static readonly char[] RecoveryCodeSeparators = { ';' };
    internal static readonly string[] DeleteClaimNames = { "UserId", "ClaimType", "ClaimValue" };
}
