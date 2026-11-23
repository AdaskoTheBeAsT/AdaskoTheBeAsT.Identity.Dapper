using System;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Hosting;

public sealed class JwtStoreCleanupHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<IdentityJwtSqlServerOptions> _optionsMonitor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<JwtStoreCleanupHostedService> _logger;

    public JwtStoreCleanupHostedService(
        IServiceProvider serviceProvider,
        IOptionsMonitor<IdentityJwtSqlServerOptions> optionsMonitor,
        TimeProvider timeProvider,
        ILogger<JwtStoreCleanupHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _serviceProvider = serviceProvider;
        _optionsMonitor = optionsMonitor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RunCleanupCycleAsync(CancellationToken cancellationToken)
    {
        var batchSize = _optionsMonitor.CurrentValue.CleanupBatchSize;
        if (batchSize <= 0)
        {
            return;
        }

        var refreshStore = _serviceProvider.GetService<IRefreshTokenStore>();
        if (refreshStore != null)
        {
            await refreshStore.PruneExpiredAsync(batchSize, cancellationToken).ConfigureAwait(false);
        }

        var mfaStore = _serviceProvider.GetService<IMfaChallengeStore>();
        if (mfaStore != null)
        {
            await mfaStore.PruneExpiredAsync(batchSize, cancellationToken).ConfigureAwait(false);
        }

        var revocationStore = _serviceProvider.GetService<ITokenRevocationStore>();
        if (revocationStore != null)
        {
            await revocationStore.PruneExpiredAsync(batchSize, cancellationToken).ConfigureAwait(false);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!options.EnableCleanupHostedService)
        {
            return;
        }

        using var timer = new PeriodicTimer(options.CleanupInterval, _timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            if (!_optionsMonitor.CurrentValue.EnableCleanupHostedService)
            {
                continue;
            }

            try
            {
                await RunCleanupCycleAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogCleanupError(_logger, ex);
            }
        }
    }

    private static void LogCleanupError(
        ILogger logger,
        Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                exception,
                "Failed to prune expired SQL Server Identity JWT store entries.");
        }
    }
}
