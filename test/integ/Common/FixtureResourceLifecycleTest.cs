using AwesomeAssertions;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;

public sealed class FixtureResourceLifecycleTest
{
    [Fact]
    public async Task FailedInitializationCleansUpOnceAndCannotBeRetried()
    {
        var initializationCount = 0;
        var cleanupCount = 0;
        var failure = new InvalidOperationException("schema failure");
        await using var lifecycle = new FixtureResourceLifecycle(
            () =>
            {
                initializationCount++;
                throw failure;
            },
            () =>
            {
                cleanupCount++;
                return Task.CompletedTask;
            });

        (await FluentActions.Awaiting(lifecycle.InitializeAsync).Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        cleanupCount.Should().Be(1);
        (await FluentActions.Awaiting(lifecycle.InitializeAsync).Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
#pragma warning disable IDISP016, IDISP017, VSTHRD103, S6966 // Intentionally exercise repeated disposal and disposed-object guards.
        await lifecycle.DisposeAsync();
        lifecycle.Dispose();
        await lifecycle.DisposeAsync();
        initializationCount.Should().Be(1);
        cleanupCount.Should().Be(1);
        await FluentActions.Awaiting(lifecycle.InitializeAsync).Should().ThrowExactlyAsync<ObjectDisposedException>();
#pragma warning restore IDISP016, IDISP017, VSTHRD103, S6966
    }

    [Fact]
    public async Task ConcurrentInitializationAndDisposalAreSerialized()
    {
        var initializationCount = 0;
        var cleanupCount = 0;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var lifecycle = new FixtureResourceLifecycle(
            async () =>
            {
                Interlocked.Increment(ref initializationCount);
                started.SetResult();
#pragma warning disable VSTHRD003 // The test controls this already-started task and has no UI context.
                await finish.Task;
#pragma warning restore VSTHRD003
            },
            () =>
            {
                Interlocked.Increment(ref cleanupCount);
                return Task.CompletedTask;
            });

#pragma warning disable IDISP013 // All initialization callbacks are awaited by WhenAll before the lifecycle leaves scope.
        var initializing = Enumerable.Range(0, 8).Select(_ => lifecycle.InitializeAsync()).ToArray();
#pragma warning restore IDISP013
#pragma warning disable VSTHRD003 // Coordinate an already-started fixture task without a UI context.
        await started.Task;
#pragma warning restore VSTHRD003
#pragma warning disable IDISP013 // All disposal callbacks are awaited by WhenAll before the lifecycle leaves scope.
        var disposing = Enumerable.Range(0, 8).Select(_ => lifecycle.DisposeAsync().AsTask()).ToArray();
#pragma warning restore IDISP013
        disposing.Should().AllSatisfy(task => task.IsCompleted.Should().BeFalse());
        cleanupCount.Should().Be(0);
        finish.SetResult();
        await Task.WhenAll(initializing);
        await Task.WhenAll(disposing);
#pragma warning disable IDISP017, VSTHRD103, S6966 // Verify the synchronous bridge after concurrent asynchronous disposal.
        lifecycle.Dispose();
#pragma warning restore IDISP017, VSTHRD103, S6966
        initializationCount.Should().Be(1);
        cleanupCount.Should().Be(1);
    }

    [Fact]
    public async Task DisposalBeforeInitializationNeverStartsTheResource()
    {
        var initializationCount = 0;
        var cleanupCount = 0;
        await using var lifecycle = new FixtureResourceLifecycle(
            () =>
            {
                initializationCount++;
                return Task.CompletedTask;
            },
            () =>
            {
                cleanupCount++;
                return Task.CompletedTask;
            });
#pragma warning disable IDISP016, IDISP017, VSTHRD103, S6966 // Intentionally dispose before initialization and then verify rejection.
        lifecycle.Dispose();
        await lifecycle.DisposeAsync();
        await FluentActions.Awaiting(lifecycle.InitializeAsync).Should().ThrowExactlyAsync<ObjectDisposedException>();
#pragma warning restore IDISP016, IDISP017, VSTHRD103, S6966
        initializationCount.Should().Be(0);
        cleanupCount.Should().Be(1);
    }

    [Fact]
    public async Task CleanupFailureDoesNotReplaceInitializationFailureOrRunTwice()
    {
        var failure = new InvalidOperationException("initialization failure");
        var cleanupFailure = new IOException("cleanup failure");
        var cleanupCount = 0;
#pragma warning disable CA2000 // Dispose objects before losing scope
        var lifecycle = new FixtureResourceLifecycle(
            () => Task.FromException(failure),
            () =>
            {
                cleanupCount++;
                throw cleanupFailure;
            });
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning disable IDISP016 // Each callback obtains a fresh ValueTask while exercising cached disposal failures.
        Func<Task> disposeAsync = () => lifecycle.DisposeAsync().AsTask();
#pragma warning restore IDISP016
        try
        {
            var actual = (await FluentActions.Awaiting(lifecycle.InitializeAsync).Should().ThrowExactlyAsync<InvalidOperationException>()).Which;
            actual.Should().BeSameAs(failure);
            actual.Data[FixtureResourceLifecycle.CleanupFailureKey].Should().BeSameAs(cleanupFailure);
            (await FluentActions.Awaiting(disposeAsync).Should().ThrowExactlyAsync<IOException>()).Which.Should().BeSameAs(cleanupFailure);
            FluentActions.Invoking(lifecycle.Dispose).Should().ThrowExactly<IOException>().Which.Should().BeSameAs(cleanupFailure);
            cleanupCount.Should().Be(1);
        }
        finally
        {
            await FluentActions.Awaiting(disposeAsync).Should().ThrowExactlyAsync<IOException>();
        }
    }

    [Fact]
    public async Task DisposalDuringFailedInitializationWaitsForSingleCleanup()
    {
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("schema failure");
        var cleanupCount = 0;
        await using var lifecycle = new FixtureResourceLifecycle(
            async () =>
            {
#pragma warning disable VSTHRD003 // Wait for the test-owned gate rather than starting UI-affine work.
                await finish.Task;
#pragma warning restore VSTHRD003
                throw failure;
            },
            async () =>
            {
                cleanupCount++;
                cleanupStarted.SetResult();
#pragma warning disable VSTHRD003 // Wait for the test-owned cleanup gate without a UI context.
                await cleanupFinished.Task;
#pragma warning restore VSTHRD003
            });
        var initialization = lifecycle.InitializeAsync();
#pragma warning disable IDISP016 // Start disposal while initialization is still running to test cleanup serialization.
        var disposal = lifecycle.DisposeAsync().AsTask();
#pragma warning restore IDISP016
        finish.SetResult();
#pragma warning disable VSTHRD003 // Observe the already-started cleanup attempt.
        await cleanupStarted.Task;
#pragma warning restore VSTHRD003
        initialization.IsCompleted.Should().BeFalse();
        disposal.IsCompleted.Should().BeFalse();
        cleanupFinished.SetResult();
#pragma warning disable VSTHRD003 // Observe the initialization task started earlier by this test.
        (await FluentActions.Awaiting(() => initialization).Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
#pragma warning restore VSTHRD003
        await disposal;
#pragma warning disable IDISP017, VSTHRD103, S6966 // Verify synchronous disposal after the asynchronous failure path.
        lifecycle.Dispose();
#pragma warning restore IDISP017, VSTHRD103, S6966
        cleanupCount.Should().Be(1);
    }
}
