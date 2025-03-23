using AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Util;
using DbUp;
using DbUp.Oracle;
using Testcontainers.Oracle;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.TestCollections;

public sealed class DatabaseWithGuidIdFixture
    : IAsyncLifetime,
        IDisposable
{
    private const string DbName = "XEPDB1";

    private readonly OracleContainer _oracleContainer
        = new OracleBuilder()
            .WithPassword("TestPass123!")
            .WithPortBinding(64797, 1521)
            .Build();

    public DatabaseWithGuidIdFixture()
    {
        OracleDapperConfig.ConfigureTypeHandlers();
        //OracleDapperConfig.ConfigureTypeHandlers(BooleanAs.Char);
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
            await _oracleContainer.StartAsync();
            var path = Path.Combine("Scripts", "WithoutNormalizedAspNetIdentityGuid.sql");
#pragma warning disable SCS0018
            var content = await File.ReadAllTextAsync(path);
#pragma warning restore SCS0018
            ConnectionString = _oracleContainer.GetConnectionString();
            var upgradeEngineBuilder = DeployChanges.To
                .OracleDatabaseWithDefaultDelimiter(ConnectionString)
                .WithScript(
                    "Script_000001_Init", content)
                .LogTo(TestOutputHelperAdapter);

            var upgradeEngine = upgradeEngineBuilder.Build();

            var result = upgradeEngine.PerformUpgrade();

            var msg = result.Successful
                ? "Successfully ran migrations"
                : $"Failed to run migrations {result?.Error}";
            TestOutputHelperAdapter.LogInformation($"final {msg}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public Task DisposeAsync() => _oracleContainer.DisposeAsync().AsTask();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
}
