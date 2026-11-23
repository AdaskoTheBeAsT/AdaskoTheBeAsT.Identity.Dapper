using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal sealed class RelationalCleanup(
    RelationalDatabase database,
    ILogger<RelationalCleanup> logger,
    IEnumerable<AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh.IRefreshTokenStore> refresh,
    IEnumerable<AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa.IMfaChallengeStore> challenges) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = database.Options;
        if (!options.EnableCleanupHostedService)
        {
            return;
        }

        using var timer = new PeriodicTimer(options.CleanupInterval, database.Clock);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                if (refresh.OfType<RelationalRefreshStore>().LastOrDefault() is { } refreshStore)
                {
                    await refreshStore.PruneExpiredAsync(options.CleanupBatchSize, stoppingToken).ConfigureAwait(false);
                }

                if (challenges.OfType<RelationalMfaStore>().LastOrDefault() is { } mfaStore)
                {
                    await mfaStore.PruneExpiredAsync(options.CleanupBatchSize, stoppingToken).ConfigureAwait(false);
                }

                await RelationalRows.PruneAsync(database, "revocations", options.CleanupBatchSize, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ex, "Failed to prune expired Identity JWT store entries.");
                }
            }
        }
    }
}
