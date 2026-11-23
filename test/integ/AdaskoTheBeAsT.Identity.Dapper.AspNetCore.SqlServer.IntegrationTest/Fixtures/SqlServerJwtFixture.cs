using System.Text.RegularExpressions;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Schema;
using Dapper;
using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.MsSql;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest.Fixtures;

public sealed class SqlServerJwtFixture : IAsyncLifetime
{
    public const string CustomSchemaName = "auth_custom";

    private static readonly Regex GoBatchSplitter = new(
        @"^\s*GO\s*$",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(5));

    private readonly string _dbName = $"IdentityJwtIntegDb_{Guid.NewGuid():N}";

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2213:Disposable fields should be disposed",
        Justification = "Disposed in DisposeAsync via IAsyncLifetime.")]
    private MsSqlContainer? _container;
    private string? _localDbMasterConnectionString;

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var masterConnectionString = await AcquireMasterConnectionStringAsync(ct);

        await using (var masterConn = new SqlConnection(masterConnectionString))
        {
            await masterConn.OpenAsync(ct);
            await masterConn.ExecuteAsync($"CREATE DATABASE [{_dbName}];");
        }

        var builder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = _dbName,
        };
        ConnectionString = builder.ConnectionString;

        await InitializeDatabaseObjectsAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
            return;
        }

        if (!string.IsNullOrWhiteSpace(_localDbMasterConnectionString))
        {
            SqlConnection.ClearAllPools();
            await using var masterConn = new SqlConnection(_localDbMasterConnectionString);
            await masterConn.OpenAsync();
            await masterConn.ExecuteAsync(
                $"IF DB_ID(N'{_dbName}') IS NOT NULL BEGIN ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_dbName}]; END;");
        }
    }

    internal SqlServerJwtStoreScope CreateStoreScope(
        FakeTimeProvider? timeProvider = null,
        string schemaName = "dbo",
        int cleanupBatchSize = 500,
        string? identitySchemaName = null) =>
        new(ConnectionString, timeProvider ?? new FakeTimeProvider(DateTimeOffset.UtcNow), schemaName, cleanupBatchSize, identitySchemaName);

    private static MsSqlContainer BuildMsSqlContainer() =>
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithExposedPort(1433)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilCommandIsCompleted(
                        "/opt/mssql-tools18/bin/sqlcmd",
                        "-C",
                        "-Q",
                        "SELECT 1;"))
            .WithPassword("TestPass123!")
            .Build();

    private static async Task ExecuteSqlBatchesAsync(
        SqlConnection connection,
        string scriptText,
        CancellationToken ct)
    {
        var batches = GoBatchSplitter
            .Split(scriptText)
            .Select(b => b.Trim())
            .Where(b => !string.IsNullOrWhiteSpace(b));

        foreach (var batch in batches)
        {
            await connection.ExecuteAsync(new CommandDefinition(batch, cancellationToken: ct));
        }
    }

    private static async Task<string> ReadEmbeddedScriptAsync(string resourceName)
    {
        await using var stream = typeof(SqlServerJwtFixture).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded SQL script: {resourceName}");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private async Task<string> AcquireMasterConnectionStringAsync(CancellationToken ct)
    {
        try
        {
            _container = BuildMsSqlContainer();
            await _container.StartAsync(ct);
            return _container.GetConnectionString();
        }
        catch (Exception ex) when (
            OperatingSystem.IsWindows() &&
            (ex is DockerUnavailableException || ex is Docker.DotNet.DockerApiException))
        {
            if (_container != null)
            {
                await _container.DisposeAsync();
                _container = null;
            }

            _localDbMasterConnectionString =
                "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=True;TrustServerCertificate=True;";
            return _localDbMasterConnectionString;
        }
    }

    private async Task InitializeDatabaseObjectsAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync(ct);

        await ExecuteSqlBatchesAsync(conn, await ReadEmbeddedScriptAsync("SqlScripts.IdentityJwtRefreshTokens.sql"), ct);
        await ExecuteSqlBatchesAsync(conn, await ReadEmbeddedScriptAsync("SqlScripts.IdentityJwtMfaChallenges.sql"), ct);
        await ExecuteSqlBatchesAsync(conn, await ReadEmbeddedScriptAsync("SqlScripts.IdentityJwtRevocations.sql"), ct);
        await ExecuteSqlBatchesAsync(conn, await ReadEmbeddedScriptAsync("SqlScripts.AspNetClaimKinds.sql"), ct);
        await ExecuteSqlBatchesAsync(conn, await ReadEmbeddedScriptAsync("SqlScripts.AspNetClaims.sql"), ct);

        const string dboIdentityAndSeedSql = """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetClaimKinds] WHERE [Name] = N'FrontendClaim')
                INSERT INTO [dbo].[AspNetClaimKinds] ([Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive])
                VALUES (N'FrontendClaim', N'Claims emitted into frontend JWT access tokens and available to backend endpoints.', 1, 1, 1);

            IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetClaimKinds] WHERE [Name] = N'BackendClaim')
                INSERT INTO [dbo].[AspNetClaimKinds] ([Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive])
                VALUES (N'BackendClaim', N'Backend authorization claims resolved server-side via IClaimsTransformation.', 0, 1, 1);

            IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetClaimKinds] WHERE [Name] = N'DataClaim')
                INSERT INTO [dbo].[AspNetClaimKinds] ([Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive])
                VALUES (N'DataClaim', N'Row/resource scope data claims resolved server-side via IClaimsTransformation.', 0, 1, 1);

            CREATE TABLE [dbo].[AspNetRoles] (
                [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(256) NULL,
                [NormalizedName] NVARCHAR(256) NULL,
                [ConcurrencyStamp] NVARCHAR(MAX) NULL
            );

            CREATE TABLE [dbo].[AspNetUserRoles] (
                [UserId] UNIQUEIDENTIFIER NOT NULL,
                [RoleId] UNIQUEIDENTIFIER NOT NULL,
                CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId])
            );

            CREATE TABLE [dbo].[AspNetUserClaims] (
                [Id] INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
                [UserId] UNIQUEIDENTIFIER NOT NULL,
                [ClaimType] NVARCHAR(MAX) NULL,
                [ClaimValue] NVARCHAR(MAX) NULL
            );

            CREATE TABLE [dbo].[AspNetRoleClaims] (
                [Id] INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
                [RoleId] UNIQUEIDENTIFIER NOT NULL,
                [ClaimType] NVARCHAR(MAX) NULL,
                [ClaimValue] NVARCHAR(MAX) NULL
            );
            """;
        await conn.ExecuteAsync(new CommandDefinition(dboIdentityAndSeedSql, cancellationToken: ct));

        var customSchemaSql = SqlServerJwtSchemaHelper.BuildCreateSchemaAndTablesSql(
            new IdentityJwtSqlServerOptions
            {
                ConnectionString = ConnectionString,
                SchemaName = CustomSchemaName,
            });
        await conn.ExecuteAsync(new CommandDefinition(customSchemaSql, cancellationToken: ct));
    }
}
