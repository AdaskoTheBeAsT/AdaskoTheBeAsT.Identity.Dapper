using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Util;
using DbUp;
using DbUp.SQLite.Helpers;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.TestCollections;

public sealed class DatabaseWithGuidIdFixture
    : IAsyncLifetime,
        IDisposable
{
    public DatabaseWithGuidIdFixture()
    {
        Connection = new SqliteConnection("Data Source=:memory:;Version=3;");

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        InitializeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    public SqliteConnection Connection { get; private set; }

    public TestOutputHelperAdapter TestOutputHelperAdapter { get; } = new();

    public async Task InitializeAsync()
    {
        try
        {
            var path = Path.Combine("Scripts", "WithoutNormalizedAspNetIdentityGuid.sql");
#pragma warning disable SCS0018
            var content = await File.ReadAllTextAsync(path);
#pragma warning restore SCS0018

            using var sharedConnection = new SharedConnection(Connection);
            var upgradeEngineBuilder = DeployChanges.To
                .SQLiteDatabase(sharedConnection)
                .WithScript(
                    "Script_000001_Init", content)
                .LogTo(TestOutputHelperAdapter);

            var upgradeEngine = upgradeEngineBuilder.Build();

            var result = upgradeEngine.PerformUpgrade();
            var msg = result.Successful
                ? "Successfully ran migrations"
                : $"Failed to run migrations {result.Error}";
            TestOutputHelperAdapter.WriteInformation($"final {msg}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public Task DisposeAsync() => Connection.DisposeAsync().AsTask();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
}
