using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Util;
using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Testcontainers.MsSql;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.TestCollections;

public sealed class DatabaseWithGuidIdFixture
    : IAsyncLifetime,
        IDisposable
{
    private const string DbName = "WithoutNormalizedAspNetIdentityGuid";
    private readonly FixtureResourceLifecycle _lifecycle;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "FixtureResourceLifecycle invokes CleanupAsync exactly once.")]
    private readonly MsSqlContainer _msSqlContainer
        = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithExposedPort(1433)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilCommandIsCompleted(
                        "/opt/mssql-tools18/bin/sqlcmd",
                        "-C",
                        "-Q",
                        "SELECT 1;"
                    )
            )
            //.WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            //.WithEnvironment("ACCEPT_EULA", "Y")
            //.WithEnvironment("MSSQL_SA_PASSWORD", "TestPass123!")
            //.WithEnvironment("MSSQL_PID", "Developer")
            //.WithExposedPort(55123)
            .WithPassword("TestPass123!")
            .Build();

    public static DatabaseWithGuidIdFixture Shared { get; } = new();

    public DatabaseWithGuidIdFixture()
    {
        _lifecycle = new FixtureResourceLifecycle(InitializeCoreAsync, CleanupAsync);
    }

    public string ConnectionString { get; set; } = string.Empty;

    public ValueTask InitializeAsync() => new(_lifecycle.InitializeAsync());

    private async Task InitializeCoreAsync()
    {
        await _msSqlContainer.StartAsync();
        ConnectionString = _msSqlContainer.GetConnectionString();
        await using var connection = new SqlConnection(ConnectionString);
        await CreateDbAsync(connection);
        var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(ConnectionString)
        {
            InitialCatalog = DbName,
        };
        ConnectionString = sqlConnectionStringBuilder.ConnectionString;
    }

    public ValueTask DisposeAsync() => _lifecycle.DisposeAsync();

    public void Dispose() => _lifecycle.Dispose();

    private Task CleanupAsync() => _msSqlContainer.DisposeAsync().AsTask();

    private Task CreateDbAsync(SqlConnection connection)
    {
        var initScriptPath =
            Path.GetFullPath(Path.Combine("Scripts", DbName, "init.sql"));
        var server = new Server(new ServerConnection(connection));
        var content = InitScriptProcessor.PreprocessInitScript(initScriptPath, DbName);
        server.ConnectionContext.ExecuteNonQuery(content);
        return Task.CompletedTask;
    }
}
