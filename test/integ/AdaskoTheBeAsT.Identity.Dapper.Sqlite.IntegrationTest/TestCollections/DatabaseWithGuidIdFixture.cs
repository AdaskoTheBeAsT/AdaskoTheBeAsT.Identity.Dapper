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
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;
    private readonly string _databasePath;

    public static DatabaseWithGuidIdFixture Shared { get; } = new();

    public DatabaseWithGuidIdFixture()
    {
        SQLitePCL.Batteries.Init();
        SqliteDapperConfig.ConfigureTypeHandlers();
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"AdaskoTheBeAsT.Identity.Dapper.{Guid.NewGuid():N}.db");
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ConnectionString;
    }

    public string ConnectionString { get; }

    public TestOutputHelperAdapter TestOutputHelperAdapter { get; } = new();

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            var path = Path.Combine("Scripts", "WithoutNormalizedAspNetIdentityGuid.sql");
#pragma warning disable SCS0018
            var content = await File.ReadAllTextAsync(path);
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
            _initialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public Task DisposeAsync()
    {
        _initializationLock.Dispose();

        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return Task.CompletedTask;
    }

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
}
