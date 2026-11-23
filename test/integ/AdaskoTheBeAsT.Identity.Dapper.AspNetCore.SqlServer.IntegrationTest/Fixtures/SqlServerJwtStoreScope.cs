using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest.Fixtures;

internal sealed class SqlServerJwtStoreScope : IDisposable, IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    public SqlServerJwtStoreScope(
        string connectionString,
        FakeTimeProvider timeProvider,
        string schemaName,
        int cleanupBatchSize,
        string? identitySchemaName = null)
    {
        TimeProvider = timeProvider;
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<TimeProvider>(timeProvider);

        services
            .AddIdentityJwtValidation(ConfigureDefaultValidationOptions)
            .AddSqlServerTokenRevocationStore(sql =>
            {
                sql.ConnectionString = connectionString;
                sql.SchemaName = schemaName;
                sql.IdentitySchemaName = identitySchemaName;
                sql.EnableCleanupHostedService = false;
                sql.CleanupBatchSize = cleanupBatchSize;
            });

        services.AddSingleton<IRefreshTokenStore, AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Stores.SqlServerRefreshTokenStore>();
        services.AddSingleton<IMfaChallengeStore, AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Stores.SqlServerMfaChallengeStore>();

        _provider = services.BuildServiceProvider();
        RefreshStore = _provider.GetRequiredService<IRefreshTokenStore>();
        MfaStore = _provider.GetRequiredService<IMfaChallengeStore>();
        RevocationStore = _provider.GetRequiredService<ITokenRevocationStore>();
        ClaimCatalogStore = _provider.GetRequiredService<IJwtClaimCatalogStore>();
        CleanupService = _provider.GetServices<IHostedService>().OfType<JwtStoreCleanupHostedService>().Single();
    }

    public FakeTimeProvider TimeProvider { get; }

    public IRefreshTokenStore RefreshStore { get; }

    public IMfaChallengeStore MfaStore { get; }

    public ITokenRevocationStore RevocationStore { get; }

    public IJwtClaimCatalogStore ClaimCatalogStore { get; }

    public JwtStoreCleanupHostedService CleanupService { get; }

    public void Dispose() => _provider.Dispose();

    public ValueTask DisposeAsync() => _provider.DisposeAsync();

    private static void ConfigureDefaultValidationOptions(IdentityJwtValidationOptions jwt)
    {
        jwt.Issuer = "https://sql-integ.example.test";
        jwt.Audiences.Add("https://sql-api.example.test");
        jwt.SigningKeys.Add(
            new JwtSigningKeyDescriptor
            {
                Kid = "sql-hmac",
                Algorithm = JwtKeyAlgorithm.HS256,
                SymmetricKey = Convert.ToBase64String(new byte[32]),
            });
    }
}
