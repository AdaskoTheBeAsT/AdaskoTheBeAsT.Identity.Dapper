using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Util;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql;
using DbUp;
using Testcontainers.PostgreSql;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.TestCollections;

public sealed class DatabaseWithGuidIdFixture
    : IAsyncLifetime,
        IDisposable
{
    private const string DbName = "WithoutNormalizedAspNetIdentityGuid";
    private readonly FixtureResourceLifecycle _lifecycle;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "FixtureResourceLifecycle invokes CleanupAsync exactly once.")]
    private readonly PostgreSqlContainer _postgreSqlContainer
        = new PostgreSqlBuilder("postgres:18.3")
            .WithDatabase(DbName)
            .WithUsername("admin")
            .WithPassword("TestPass123!")
            .Build();

    public static DatabaseWithGuidIdFixture Shared { get; } = new();

    public DatabaseWithGuidIdFixture()
    {
        _lifecycle = new FixtureResourceLifecycle(InitializeCoreAsync, CleanupAsync);
        PostgreSqlDapperConfig.ConfigureTypeHandlers();
    }

    public string ConnectionString { get; set; } = string.Empty;

    public TestOutputHelperAdapter TestOutputHelperAdapter { get; } = new();

    public ValueTask InitializeAsync() => new(_lifecycle.InitializeAsync());

    private async Task InitializeCoreAsync()
    {
        await _postgreSqlContainer.StartAsync();
        var path = Path.Combine("Scripts", "WithoutNormalizedAspNetIdentityGuid.sql");
#pragma warning disable SCS0018
        var content = await File.ReadAllTextAsync(path);
#pragma warning restore SCS0018
        ConnectionString = _postgreSqlContainer.GetConnectionString();
        var upgradeEngineBuilder = DeployChanges.To
            .PostgresqlDatabase(ConnectionString, "public")
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
            throw new InvalidOperationException("PostgreSQL test schema initialization failed.", result.Error);
        }
    }

    public ValueTask DisposeAsync() => _lifecycle.DisposeAsync();

    public void Dispose() => _lifecycle.Dispose();

    private Task CleanupAsync() => _postgreSqlContainer.DisposeAsync().AsTask();
}
