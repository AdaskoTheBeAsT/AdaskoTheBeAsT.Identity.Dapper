using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.TestCollections;
using Reqnroll;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Hooks;

[Binding]
public static class DatabaseWithGuidIdHooks
{
    [BeforeTestRun(Order = 0)]
    public static Task BeforeTestRunAsync() => DatabaseWithGuidIdFixture.Shared.InitializeAsync().AsTask();

    [AfterTestRun(Order = 1000)]
    public static Task AfterTestRunAsync() => DatabaseWithGuidIdFixture.Shared.DisposeAsync().AsTask();
}
