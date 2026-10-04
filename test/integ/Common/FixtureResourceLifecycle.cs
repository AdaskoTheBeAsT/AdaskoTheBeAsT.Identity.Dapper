namespace AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;

/// <summary>Owns one initialization attempt and one cleanup attempt, including failed initialization.</summary>
internal sealed class FixtureResourceLifecycle(Func<Task> initialize, Func<Task> cleanup)
    : IAsyncDisposable, IDisposable
{
    internal const string CleanupFailureKey = "FixtureCleanupException";
    private readonly object _gate = new();
    private Task? _initializationTask;
    private Task? _cleanupTask;
    private Task? _disposalTask;

    public Task InitializeAsync()
    {
        lock (_gate)
        {
            if (_disposalTask != null)
            {
                return Task.FromException(new ObjectDisposedException(nameof(FixtureResourceLifecycle)));
            }

            // Cache failures too: never retry against a partially initialized resource.
            return _initializationTask ??= InitializeCoreAsync();
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            return new ValueTask(_disposalTask ??= DisposeCoreAsync(_initializationTask));
        }
    }

#pragma warning disable VSTHRD002 // Required synchronous IDisposable bridge; lifecycle awaits do not capture context.
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

    private async Task InitializeCoreAsync()
    {
        try
        {
            await initialize().ConfigureAwait(false);
        }
        catch (Exception initializationFailure)
        {
            try
            {
                await CleanupOnceAsync().ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                // Keep the original exception/stack while retaining the cleanup failure for diagnosis.
                initializationFailure.Data[CleanupFailureKey] = cleanupFailure;
            }

            throw;
        }
    }

    private async Task DisposeCoreAsync(Task? initialization)
    {
        if (initialization != null)
        {
            try
            {
                await initialization.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Initialization reports its own failure; disposal must still observe cleanup.
            }
        }

        await CleanupOnceAsync().ConfigureAwait(false);
    }

    private Task CleanupOnceAsync()
    {
        lock (_gate)
        {
            return _cleanupTask ??= CleanupCoreAsync();
        }
    }

    private async Task CleanupCoreAsync() => await cleanup().ConfigureAwait(false);
}
