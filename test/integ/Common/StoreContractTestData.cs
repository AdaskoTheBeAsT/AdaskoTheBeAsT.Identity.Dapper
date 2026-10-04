namespace AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;

internal static class StoreContractTestData
{
    internal static readonly string[] RecoveryCodes = { "code-1", "code-2", "code-3" };
    internal static readonly string[] RemainingClaims = { "value-66", "value-67", "value-68", "value-69" };
    internal static readonly string[] CaseVariantRecoveryCodes = { "one", "ONE", "two" };
    internal static readonly string[] ConcurrentRecoveryCodes = { "first", "second" };
}
