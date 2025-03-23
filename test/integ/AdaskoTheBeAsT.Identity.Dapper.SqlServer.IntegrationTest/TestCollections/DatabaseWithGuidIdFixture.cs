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

    private readonly MsSqlContainer _msSqlContainer
        = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithPortBinding(55123, 1433)
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

    public DatabaseWithGuidIdFixture()
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        InitializeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    public string ConnectionString { get; set; } = string.Empty;

    public async Task InitializeAsync()
    {
        try
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
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public Task DisposeAsync() => _msSqlContainer.DisposeAsync().AsTask();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

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
