using AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.TestCollections;
using Reqnroll;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Hooks;

[Binding]
public sealed class DatabaseWithGuidIdHooks
{
    [BeforeTestRun(Order = 0)]
    public static Task BeforeTestRunAsync() => DatabaseWithGuidIdFixture.Shared.InitializeAsync();

    [AfterTestRun(Order = 1000)]
    public static Task AfterTestRunAsync() => DatabaseWithGuidIdFixture.Shared.DisposeAsync();
}
