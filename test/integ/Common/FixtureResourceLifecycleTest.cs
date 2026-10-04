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
        await lifecycle.DisposeAsync();
        lifecycle.Dispose();
        await lifecycle.DisposeAsync();
        initializationCount.Should().Be(1);
        cleanupCount.Should().Be(1);
        await FluentActions.Awaiting(lifecycle.InitializeAsync).Should().ThrowExactlyAsync<ObjectDisposedException>();
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
                await finish.Task;
            },
            () =>
            {
                Interlocked.Increment(ref cleanupCount);
                return Task.CompletedTask;
            });

        var initializing = Enumerable.Range(0, 8).Select(_ => lifecycle.InitializeAsync()).ToArray();
        await started.Task;
        var disposing = Enumerable.Range(0, 8).Select(_ => lifecycle.DisposeAsync().AsTask()).ToArray();
        disposing.Should().AllSatisfy(task => task.IsCompleted.Should().BeFalse());
        cleanupCount.Should().Be(0);
        finish.SetResult();
        await Task.WhenAll(initializing);
        await Task.WhenAll(disposing);
        lifecycle.Dispose();
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
        lifecycle.Dispose();
        await lifecycle.DisposeAsync();
        await FluentActions.Awaiting(lifecycle.InitializeAsync).Should().ThrowExactlyAsync<ObjectDisposedException>();
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
        try
        {
            var actual = (await FluentActions.Awaiting(lifecycle.InitializeAsync).Should().ThrowExactlyAsync<InvalidOperationException>()).Which;
            actual.Should().BeSameAs(failure);
            actual.Data[FixtureResourceLifecycle.CleanupFailureKey].Should().BeSameAs(cleanupFailure);
            (await FluentActions.Awaiting(() => lifecycle.DisposeAsync().AsTask()).Should().ThrowExactlyAsync<IOException>()).Which.Should().BeSameAs(cleanupFailure);
            FluentActions.Invoking(lifecycle.Dispose).Should().ThrowExactly<IOException>().Which.Should().BeSameAs(cleanupFailure);
            cleanupCount.Should().Be(1);
        }
        finally
        {
            await FluentActions.Awaiting(() => lifecycle.DisposeAsync().AsTask()).Should().ThrowExactlyAsync<IOException>();
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
                await finish.Task;
                throw failure;
            },
            async () =>
            {
                cleanupCount++;
                cleanupStarted.SetResult();
                await cleanupFinished.Task;
            });
        var initialization = lifecycle.InitializeAsync();
        var disposal = lifecycle.DisposeAsync().AsTask();
        finish.SetResult();
        await cleanupStarted.Task;
        initialization.IsCompleted.Should().BeFalse();
        disposal.IsCompleted.Should().BeFalse();
        cleanupFinished.SetResult();
        (await FluentActions.Awaiting(() => initialization).Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        await disposal;
        lifecycle.Dispose();
        cleanupCount.Should().Be(1);
    }
}
