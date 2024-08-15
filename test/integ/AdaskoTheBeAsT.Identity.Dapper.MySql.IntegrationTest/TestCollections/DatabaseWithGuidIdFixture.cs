using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Util;
using Dapper;
using DbUp;
using Testcontainers.MySql;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.TestCollections;

public sealed class DatabaseWithGuidIdFixture
    : IAsyncLifetime,
        IDisposable
{
    private const string DbName = "WithoutNormalizedAspNetIdentityGuid";

    private readonly MySqlContainer _mySqlContainer
        = new MySqlBuilder()
            .WithImage("mysql:9.0.1")
            .WithDatabase(DbName)
            .WithExposedPort(33060)
            .WithUsername("root")
            .WithPassword("TestPass123!")
            .Build();

    public DatabaseWithGuidIdFixture()
    {
        MySqlDapperConfig.ConfigureTypeHandlers();
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
            await _mySqlContainer.StartAsync();
            var path = Path.Combine("Scripts", "WithoutNormalizedAspNetIdentityGuid.sql");
#pragma warning disable SCS0018
            var content = await File.ReadAllTextAsync(path);
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
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public Task DisposeAsync() => _mySqlContainer.DisposeAsync().AsTask();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
}
