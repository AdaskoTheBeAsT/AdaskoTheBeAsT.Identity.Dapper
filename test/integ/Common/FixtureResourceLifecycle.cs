namespace AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;

/// <summary>Owns one initialization attempt and one cleanup attempt, including failed initialization.</summary>
internal sealed class FixtureResourceLifecycle(Func<Task> initialize, Func<Task> cleanup)
    : IAsyncDisposable, IDisposable
{
    internal const string CleanupFailureKey = "FixtureCleanupException";
    private readonly Lock _gate = new();
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
            await (initialize?.Invoke() ?? throw new ArgumentNullException(nameof(initialize))).ConfigureAwait(false);
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
#pragma warning disable VSTHRD003 // Observe the cached initialization task; this fixture has no UI context.
                await initialization.ConfigureAwait(false);
#pragma warning restore VSTHRD003
            }
            catch (Exception)
            {
                // Initialization reports its own failure; disposal must still observe cleanup.
                await CleanupOnceAsync().ConfigureAwait(false);
                return;
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

    private Task CleanupCoreAsync()
    {
        try
        {
            return cleanup?.Invoke() ?? throw new ArgumentNullException(nameof(cleanup));
        }
        catch (Exception exception)
        {
            // Cache synchronous callback failures just like asynchronous cleanup failures.
            return Task.FromException(exception);
        }
    }
}
