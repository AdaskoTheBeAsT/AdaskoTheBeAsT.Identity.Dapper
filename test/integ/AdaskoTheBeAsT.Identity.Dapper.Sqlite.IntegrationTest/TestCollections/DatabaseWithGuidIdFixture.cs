using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Util;
using DbUp;
using DbUp.Sqlite.Helpers;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.TestCollections;

public sealed class DatabaseWithGuidIdFixture
    : IAsyncLifetime,
        IDisposable
{
    private readonly FixtureResourceLifecycle _lifecycle;
    private readonly string _databasePath;

    public DatabaseWithGuidIdFixture()
    {
        _lifecycle = new FixtureResourceLifecycle(InitializeCoreAsync, CleanupAsync);
        SQLitePCL.Batteries.Init();
        SqliteDapperConfig.ConfigureTypeHandlers();
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"AdaskoTheBeAsT.Identity.Dapper.{Guid.NewGuid():N}.db");
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ConnectionString;
    }

    public static DatabaseWithGuidIdFixture Shared { get; } = new();

    public string ConnectionString { get; }

    public TestOutputHelperAdapter TestOutputHelperAdapter { get; } = new();

    public ValueTask InitializeAsync() => new(_lifecycle.InitializeAsync());

    public ValueTask DisposeAsync() => _lifecycle.DisposeAsync();

    public void Dispose() => _lifecycle.Dispose();

    private async Task InitializeCoreAsync()
    {
        var path = Path.Combine("Scripts", "WithoutNormalizedAspNetIdentityGuid.sql");
#pragma warning disable SCS0018
        var content = await File.ReadAllTextAsync(path);
        content += """

            ALTER TABLE aspnetusers ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE aspnetusers ADD COLUMN DisplayLabel TEXT NULL;
            """;
#pragma warning restore SCS0018

        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        using var sharedConnection = new SharedConnection(connection);
        var upgradeEngineBuilder = DeployChanges.To
            .SqliteDatabase(sharedConnection)
            .WithScript(
                "Script_000001_Init", content)
            .LogTo(TestOutputHelperAdapter);

        var upgradeEngine = upgradeEngineBuilder.Build();

        var result = upgradeEngine.PerformUpgrade();
        var msg = result.Successful
            ? "Successfully ran migrations"
            : $"Failed to run migrations {result.Error}";
        TestOutputHelperAdapter.WriteInformation($"final {msg}");
        if (!result.Successful)
        {
            throw new InvalidOperationException("SQLite test schema initialization failed.", result.Error);
        }
    }

    private Task CleanupAsync()
    {
#pragma warning disable SCS0018, SEC0116 // Delete only this fixture's GUID-named temporary database, never a caller-supplied path.
        File.Delete(_databasePath);
#pragma warning restore SCS0018, SEC0116
        return Task.CompletedTask;
    }
}
