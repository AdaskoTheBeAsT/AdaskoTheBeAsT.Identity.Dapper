using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Util;
using DbUp;
using Testcontainers.MySql;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.TestCollections;

public sealed class DatabaseWithGuidIdFixture
    : IAsyncLifetime,
        IDisposable
{
    private const string DbName = "WithoutNormalizedAspNetIdentityGuid";
    private readonly FixtureResourceLifecycle _lifecycle;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "FixtureResourceLifecycle invokes CleanupAsync exactly once.")]
    private readonly MySqlContainer _mySqlContainer
        = new MySqlBuilder("mysql:9.6.0")
            .WithDatabase(DbName)
            .WithExposedPort(33060)
            .WithUsername("root")
            .WithPassword("TestPass123!")
            .Build();

    public DatabaseWithGuidIdFixture()
    {
        _lifecycle = new FixtureResourceLifecycle(InitializeCoreAsync, CleanupAsync);
        MySqlDapperConfig.ConfigureTypeHandlers();
    }

    public static DatabaseWithGuidIdFixture Shared { get; } = new();

    public string ConnectionString { get; set; } = string.Empty;

    public TestOutputHelperAdapter TestOutputHelperAdapter { get; } = new();

    public ValueTask InitializeAsync() => new(_lifecycle.InitializeAsync());

    public ValueTask DisposeAsync() => _lifecycle.DisposeAsync();

    public void Dispose() => _lifecycle.Dispose();

    private async Task InitializeCoreAsync()
    {
        await _mySqlContainer.StartAsync(Xunit.TestContext.Current.CancellationToken);
        var path = Path.Combine("Scripts", "WithoutNormalizedAspNetIdentityGuid.sql");
#pragma warning disable SCS0018
        var content = await File.ReadAllTextAsync(path, Xunit.TestContext.Current.CancellationToken);
#pragma warning restore SCS0018
        ConnectionString = _mySqlContainer.GetConnectionString();
        var upgradeEngineBuilder = DeployChanges.To
            .MySqlDatabase(ConnectionString, "WithoutNormalizedAspNetIdentityGuid")
            .WithScript(
                "Script_000001_Init", content)
            .LogTo(TestOutputHelperAdapter);

        var upgradeEngine = upgradeEngineBuilder.Build();

        var result = upgradeEngine.PerformUpgrade();

        var msg = result.Successful
            ? "Successfully ran migrations"
            : $"Failed to run migrations {result.Error}";
        TestOutputHelperAdapter.LogInformation($"final {msg}");
        if (!result.Successful)
        {
            throw new InvalidOperationException("MySQL test schema initialization failed.", result.Error);
        }
    }

    private Task CleanupAsync() => _mySqlContainer.DisposeAsync().AsTask();
}
