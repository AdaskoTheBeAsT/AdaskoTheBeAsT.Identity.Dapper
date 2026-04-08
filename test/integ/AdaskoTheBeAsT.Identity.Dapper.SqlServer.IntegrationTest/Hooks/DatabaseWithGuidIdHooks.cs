using AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.TestCollections;
using Reqnroll;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Hooks;

[Binding]
public sealed class DatabaseWithGuidIdHooks
{
    [BeforeTestRun(Order = 0)]
    public static Task BeforeTestRunAsync() => DatabaseWithGuidIdFixture.Shared.InitializeAsync();

    [AfterTestRun(Order = 1000)]
    public static Task AfterTestRunAsync() => DatabaseWithGuidIdFixture.Shared.DisposeAsync();
}
