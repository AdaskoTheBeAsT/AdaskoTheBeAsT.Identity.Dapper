using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Util;
using DbUp;
using Testcontainers.PostgreSql;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.TestCollections;

public sealed class DatabaseWithGuidIdFixture
    : IAsyncLifetime,
        IDisposable
{
    private const string DbName = "WithoutNormalizedAspNetIdentityGuid";

    private readonly PostgreSqlContainer _postgreSqlContainer
        = new PostgreSqlBuilder()
            .WithDatabase(DbName)
            .WithUsername("admin")
            .WithPassword("TestPass123!")
            .WithPortBinding(64320, 5432)
            .Build();

    public DatabaseWithGuidIdFixture()
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        InitializeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    public string ConnectionString { get; set; } = string.Empty;

    public TestOutputHelperAdapter TestOutputHelperAdapter { get; } = new();

    public async Task InitializeAsync()
    {
        try
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
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public Task DisposeAsync() => _postgreSqlContainer.DisposeAsync().AsTask();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
}
