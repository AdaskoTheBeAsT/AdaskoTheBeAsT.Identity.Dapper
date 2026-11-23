using System.Data.Common;
using System.Globalization;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySql.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySql.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySql.Schema;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySqlConnector.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySqlConnector.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.MySqlConnector.Schema;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Oracle.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Oracle.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Oracle.Schema;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.PostgreSql.Schema;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Sqlite.Schema;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.MySql;
using Testcontainers.Oracle;
using Testcontainers.PostgreSql;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational.IntegrationTest;

internal sealed class TestDatabase(
    string provider,
    string connectionString,
    IDatabaseContainer? container = null,
    string? sqliteFile = null) : IAsyncDisposable
{
    private const string GuidId = "f1c7e5cd-5162-4547-9550-ea22615d5190";

    public string UserId => provider switch
    {
        "Oracle" or "PostgreSql" => GuidId,
        "Sqlite" => "42",
        _ => "fixture-user",
    };

    public static async Task<TestDatabase> StartAsync(string provider, CancellationToken ct)
    {
        if (string.Equals(provider, "Sqlite", StringComparison.Ordinal))
        {
            SQLitePCL.Batteries.Init();
            var path = Path.GetTempFileName();
            return new TestDatabase(provider, new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString(), sqliteFile: path);
        }

        const string passwordSuffix = "Aa1";
        const int oraclePasswordMaxLength = 30;
        var passwordSeed = Guid.NewGuid().ToString("N");
        var password = passwordSeed + passwordSuffix;
        IDatabaseContainer database = provider switch
        {
            "PostgreSql" => new PostgreSqlBuilder("postgres:18.3").WithPassword(password).Build(),
            "MySql" or "MySqlConnector" => new MySqlBuilder("mysql:9.6.0").WithPassword(password).Build(),
            "Oracle" => new OracleBuilder("gvenzl/oracle-xe:21.3.0-slim-faststart")
                .WithPassword(passwordSeed[..(oraclePasswordMaxLength - passwordSuffix.Length)] + passwordSuffix)
                .Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(provider)),
        };
        try
        {
            await database.StartAsync(ct);
            return new TestDatabase(provider, database.GetConnectionString(), database);
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public static IReadOnlyList<string> BuildStatements(string provider) => provider switch
    {
        "MySql" => MySqlJwtSchemaHelper.BuildSchemaStatements(new()),
        "MySqlConnector" => MySqlConnectorJwtSchemaHelper.BuildSchemaStatements(new()),
        "Oracle" => OracleJwtSchemaHelper.BuildSchemaStatements(new()),
        "PostgreSql" => PostgreSqlJwtSchemaHelper.BuildSchemaStatements(new()),
        "Sqlite" => SqliteJwtSchemaHelper.BuildSchemaStatements(new()),
        _ => throw new ArgumentOutOfRangeException(nameof(provider)),
    };

    public Task EnsureSchemaAsync(CancellationToken ct) => provider switch
    {
        "MySql" => MySqlJwtSchemaHelper.EnsureSchemaAndTablesAsync(new IdentityJwtMySqlOptions { ConnectionString = connectionString }, cancellationToken: ct),
        "MySqlConnector" => MySqlConnectorJwtSchemaHelper.EnsureSchemaAndTablesAsync(new IdentityJwtMySqlConnectorOptions { ConnectionString = connectionString }, cancellationToken: ct),
        "Oracle" => OracleJwtSchemaHelper.EnsureSchemaAndTablesAsync(new IdentityJwtOracleOptions { ConnectionString = connectionString }, cancellationToken: ct),
        "PostgreSql" => PostgreSqlJwtSchemaHelper.EnsureSchemaAndTablesAsync(new IdentityJwtPostgreSqlOptions { ConnectionString = connectionString }, cancellationToken: ct),
        "Sqlite" => SqliteJwtSchemaHelper.EnsureSchemaAndTablesAsync(new IdentityJwtSqliteOptions { ConnectionString = connectionString }, cancellationToken: ct),
        _ => throw new InvalidOperationException("Unknown database provider."),
    };

    public ServiceProvider BuildIssuer(TimeProvider clock)
    {
        var services = new ServiceCollection().AddLogging().AddSingleton(clock);
        var builder = new IssuerBuilder(services);
        switch (provider)
        {
            case "MySql":
                builder.AddMySqlJwtStores(options =>
                {
                    options.ConnectionString = connectionString;
                });
                break;
            case "MySqlConnector":
                builder.AddMySqlConnectorJwtStores(options =>
                {
                    options.ConnectionString = connectionString;
                });
                break;
            case "Oracle":
                builder.AddOracleJwtStores(options =>
                {
                    options.ConnectionString = connectionString;
                    options.IdentityUserIdConverter = static id => Guid.Parse(id).ToByteArray();
                });
                break;
            case "PostgreSql":
                builder.AddPostgreSqlJwtStores(options =>
                {
                    options.ConnectionString = connectionString;
                    options.IdentityUserIdConverter = static id => Guid.Parse(id);
                });
                break;
            case "Sqlite":
                builder.AddSqliteJwtStores(options =>
                {
                    options.ConnectionString = connectionString;
                    options.IdentityUserIdConverter = static id => int.Parse(id, CultureInfo.InvariantCulture);
                });
                break;
            default:
                throw new InvalidOperationException("Unknown database provider.");
        }

        return services.BuildServiceProvider();
    }

    public ServiceProvider BuildValidator(TimeProvider clock)
    {
        var services = new ServiceCollection().AddLogging().AddSingleton(clock);
        var builder = services.AddIdentityJwtValidation(options =>
        {
            options.Issuer = "issuer";
            options.Audiences.Add("test");
            options.SigningKeys.Add(new JwtSigningKeyDescriptor
            {
                Kid = "test",
                Algorithm = JwtKeyAlgorithm.HS256,
                SymmetricKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            });
        }).AddClaimKindTransformation();
        switch (provider)
        {
            case "MySql":
                builder.AddMySqlTokenRevocationStore(options =>
                {
                    options.ConnectionString = connectionString;
                });
                break;
            case "MySqlConnector":
                builder.AddMySqlConnectorTokenRevocationStore(options =>
                {
                    options.ConnectionString = connectionString;
                });
                break;
            case "Oracle":
                builder.AddOracleTokenRevocationStore(options =>
                {
                    options.ConnectionString = connectionString;
                    options.IdentityUserIdConverter = static id => Guid.Parse(id).ToByteArray();
                });
                break;
            case "PostgreSql":
                builder.AddPostgreSqlTokenRevocationStore(options =>
                {
                    options.ConnectionString = connectionString;
                    options.IdentityUserIdConverter = static id => Guid.Parse(id);
                });
                break;
            case "Sqlite":
                builder.AddSqliteTokenRevocationStore(options =>
                {
                    options.ConnectionString = connectionString;
                    options.IdentityUserIdConverter = static id => int.Parse(id, CultureInfo.InvariantCulture);
                });
                break;
            default:
                throw new InvalidOperationException("Unknown database provider.");
        }

        return services.BuildServiceProvider();
    }

    public async Task SeedIdentityClaimsAsync(CancellationToken ct)
    {
        await using DbConnection connection = provider switch
        {
            "Sqlite" => new SqliteConnection(connectionString),
            "PostgreSql" => new NpgsqlConnection(connectionString),
            "Oracle" => new OracleConnection(connectionString),
            "MySql" => new global::MySql.Data.MySqlClient.MySqlConnection(connectionString),
            "MySqlConnector" => new global::MySqlConnector.MySqlConnection(connectionString),
            _ => throw new InvalidOperationException("Unknown database provider."),
        };
        var (idType, idLiteral) = provider switch
        {
            "PostgreSql" => ("UUID", $"'{GuidId}'"),
            "Oracle" => ("RAW(16)", "HEXTORAW('" + Convert.ToHexString(Guid.Parse(GuidId).ToByteArray()) + "')"),
            "Sqlite" => ("INTEGER", "42"),
            _ => ("VARCHAR(64)", "'fixture-user'"),
        };
        var textType = connection is OracleConnection ? "VARCHAR2(64)" : "VARCHAR(64)";
        await connection.OpenAsync(ct);
        foreach (var sql in new[]
        {
            $"CREATE TABLE aspnetuserclaims (userid {idType}, claimtype {textType}, claimvalue {textType})",
            $"CREATE TABLE aspnetuserroles (userid {idType}, roleid INTEGER)",
            $"CREATE TABLE aspnetroleclaims (roleid INTEGER, claimtype {textType}, claimvalue {textType})",
            $"INSERT INTO aspnetuserclaims (userid, claimtype, claimvalue) VALUES ({idLiteral}, 'sample', '1')",
            $"INSERT INTO aspnetuserroles (userid, roleid) VALUES ({idLiteral}, 7)",
            "INSERT INTO aspnetroleclaims (roleid, claimtype, claimvalue) VALUES (7, 'sample', '1')",
            "INSERT INTO aspnetroleclaims (roleid, claimtype, claimvalue) VALUES (7, 'permission', 'read')",
        })
        {
            await using var command = connection.CreateCommand();
#pragma warning disable CA2100, SCS0002 // Test schema and fixed fixture values only.
            command.CommandText = sql;
#pragma warning restore CA2100, SCS0002
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (container != null)
        {
#pragma warning disable IDISP007 // StartAsync gives this fixture ownership of its own container.
            await container.DisposeAsync();
#pragma warning restore IDISP007
        }

        if (sqliteFile != null)
        {
#pragma warning disable SCS0018, SEC0116 // StartAsync creates this unique file with Path.GetTempFileName.
            File.Delete(sqliteFile);
#pragma warning restore SCS0018, SEC0116
        }
    }

    private sealed class IssuerBuilder(IServiceCollection services) : IIdentityJwtBuilder<IdentityUser>
    {
        public IServiceCollection Services { get; } = services;
    }
}
