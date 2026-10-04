using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
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
    private readonly FixtureResourceLifecycle _lifecycle;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "FixtureResourceLifecycle invokes CleanupAsync exactly once.")]
    private readonly OracleContainer _oracleContainer
        = new OracleBuilder("gvenzl/oracle-xe:21.3.0-slim-faststart")
            .WithPassword("TestPass123!")
            .WithExposedPort(1521)
            .Build();

    public DatabaseWithGuidIdFixture()
    {
        _lifecycle = new FixtureResourceLifecycle(InitializeCoreAsync, CleanupAsync);
        OracleDapperConfig.ConfigureTypeHandlers();
    }

    public static DatabaseWithGuidIdFixture Shared { get; } = new();

    public string ConnectionString { get; set; } = string.Empty;

    public TestOutputHelperAdapter TestOutputHelperAdapter { get; } = new();

    public ValueTask InitializeAsync() => new(_lifecycle.InitializeAsync());

    public ValueTask DisposeAsync() => _lifecycle.DisposeAsync();

    public void Dispose() => _lifecycle.Dispose();

    private async Task InitializeCoreAsync()
    {
        await _oracleContainer.StartAsync(Xunit.TestContext.Current.CancellationToken);
        var path = Path.Combine("Scripts", "WithoutNormalizedAspNetIdentityGuid.sql");
#pragma warning disable SCS0018
        var content = await File.ReadAllTextAsync(path, Xunit.TestContext.Current.CancellationToken);
        content += """

            /
            ALTER TABLE ASPNETUSERS ADD (CreatedOn TIMESTAMP NULL, DisplayLabel VARCHAR2(256) NULL)
            /
            ALTER TABLE ASPNETUSERCLAIMS ADD AuditSource VARCHAR2(256)
            /
            ALTER TABLE ASPNETROLECLAIMS ADD AuditSource VARCHAR2(256)
            /
            ALTER TABLE ASPNETUSERLOGINS ADD AuditSource VARCHAR2(256)
            /
            ALTER TABLE ASPNETUSERROLES ADD AuditSource VARCHAR2(256)
            /
            ALTER TABLE ASPNETUSERTOKENS ADD AuditSource VARCHAR2(256)
            /
            """;
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
            : $"Failed to run migrations {result.Error}";
        TestOutputHelperAdapter.LogInformation($"final {msg}");
        if (!result.Successful)
        {
            throw new InvalidOperationException("Oracle test schema initialization failed.", result.Error);
        }
    }

    private Task CleanupAsync() => _oracleContainer.DisposeAsync().AsTask();
}
