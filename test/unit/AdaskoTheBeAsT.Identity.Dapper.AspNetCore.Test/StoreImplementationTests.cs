using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.DependencyInjection;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test;

public sealed class StoreImplementationTests
{
    [Fact]
    public async Task InMemoryRefreshStoreRotatesAtomicallyUnderSixteenWayParallelRaceAsync()
    {
        var time = new AdjustableTimeProvider();
        var store = new InMemoryRefreshTokenStore(time, MSOptions.Create(new IdentityJwtOptions()));
        var ct = TestContext.Current.CancellationToken;
        var now = time.GetUtcNow();

        var parent = CreateRefreshRecord("hash-parent", "sid-race", "uid-1", now);
        await store.StoreAsync(parent, ct);

        using var barrier = new Barrier(16);
        var tasks = Enumerable.Range(0, 16)
            .Select(async i => await Task.Run(
                async () =>
                {
                    barrier.SignalAndWait(ct);
                    var replacement = CreateRefreshRecord($"hash-child-{i}", "sid-race", "uid-1", now);
                    return await store.RotateAsync("hash-parent", replacement, now, ct);
                },
                ct))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        results.Count(r => r.Status == RefreshTokenRotationStatus.Rotated).Should().Be(1);
        results.Count(r => r.Status == RefreshTokenRotationStatus.Replayed).Should().Be(15);
    }

    [Fact]
    public async Task InMemoryRefreshStoreEvictsOldestEntriesAndSupportsSessionRevocationAndPruningAsync()
    {
        var time = new AdjustableTimeProvider();
        var store = new InMemoryRefreshTokenStore(
            time,
            MSOptions.Create(new IdentityJwtOptions { InMemoryRefreshTokenCapacity = 2 }));
        var ct = TestContext.Current.CancellationToken;
        var now = time.GetUtcNow();

        await store.StoreAsync(CreateRefreshRecord("h-1", "s-1", "u-1", now), ct);
        await store.StoreAsync(CreateRefreshRecord("h-2", "s-2", "u-1", now), ct);
        await store.StoreAsync(CreateRefreshRecord("h-3", "s-3", "u-1", now), ct);

        (await store.FindByHashAsync("h-1", ct)).Should().BeNull();
        (await store.FindByHashAsync("h-2", ct)).Should().NotBeNull();
        (await store.FindByHashAsync("h-3", ct)).Should().NotBeNull();

        await store.RevokeSessionAsync("s-2", now, "logout", ct);
        var rotatedRevoked = await store.RotateAsync("h-2", CreateRefreshRecord("h-2b", "s-2", "u-1", now), now, ct);
        rotatedRevoked.Status.Should().Be(RefreshTokenRotationStatus.Revoked);

        var revokedSessions = await store.RevokeAllUserSessionsAsync("u-1", now, "compromise", ct);
        revokedSessions.Should().Contain("s-3");

        time.Advance(TimeSpan.FromDays(40));
        var pruned = await store.PruneExpiredAsync(10, ct);
        pruned.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MfaChallengeStoresEnforceMaxAttemptsResendReplayAndSingleConsumeAsync(bool distributed)
    {
        var time = new AdjustableTimeProvider();
        IMfaChallengeStore store = distributed
            ? new DistributedCacheMfaChallengeStore(new TestAtomicMfaChallengeCache())
            : new InMemoryMfaChallengeStore(time, MSOptions.Create(new IdentityJwtOptions()));
        var ct = TestContext.Current.CancellationToken;
        var now = time.GetUtcNow();

        var record = new MfaChallengeRecord(
            Guid.NewGuid(),
            "ch-hash-1",
            "hk-1",
            MfaChallengePurpose.LoginChallenge,
            "user-1",
            "stamp-1",
            "Authenticator,Email",
            "Email",
            "otp-hash-initial",
            AttemptCount: 0,
            MaxAttempts: 5,
            ResendCount: 0,
            ResendAvailableAtUtc: now.AddSeconds(30),
            CreatedAtUtc: now,
            ExpiresAtUtc: now.AddMinutes(5));

        await store.StoreAsync(record, ct);
        var updatedOk = await store.UpdateOtpStateAsync(
            "ch-hash-1",
            "Email",
            "otp-hash-resent",
            "hk-1",
            resendCount: 1,
            resendAvailableAtUtc: now.AddSeconds(60),
            nowUtc: now.AddSeconds(31),
            ct);
        updatedOk.Should().BeTrue();
        var updated = await store.FindAsync("ch-hash-1", now.AddSeconds(31), ct);
        updated!.OtpCodeHash.Should().Be("otp-hash-resent");
        updated.ResendCount.Should().Be(1);

        (await store.TryRecordUsedOtpAsync("replay-hash-1", "hk-1", "user-1", now, now.AddMinutes(5), ct)).Should().BeTrue();
        (await store.TryRecordUsedOtpAsync("replay-hash-1", "hk-1", "user-1", now, now.AddMinutes(5), ct)).Should().BeFalse();

        for (var i = 1; i <= 5; i++)
        {
            var attempt = await store.IncrementAttemptAsync("ch-hash-1", now, ct);
            attempt.Status.Should().Be(MfaChallengeAttemptStatus.Allowed);
        }

        var sixthAttempt = await store.IncrementAttemptAsync("ch-hash-1", now, ct);
        sixthAttempt.Status.Should().Be(MfaChallengeAttemptStatus.Exhausted);
    }

    [Fact]
    public async Task DistributedMfaStoresShareAtomicAttemptResendConsumptionAndReplayStateAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var harness = await JwtTestHostHarness.CreateAsync(configureBuilder: builder =>
        {
            builder.Services.AddDistributedMemoryCache();
            builder.AddDistributedCacheStores<TestUser, TestAtomicMfaChallengeCache>();
        });
        var now = harness.TimeProvider.GetUtcNow();
        var provider = harness.Services;
        var first = provider.GetRequiredService<IMfaChallengeStore>();
        var second = new DistributedCacheMfaChallengeStore(provider.GetRequiredService<IAtomicMfaChallengeCache>());
        provider.GetRequiredService<ITokenRevocationStore>().Should().BeOfType<DistributedCacheTokenRevocationStore>();
        provider.GetRequiredService<IRefreshTokenStore>().Should().BeOfType<InMemoryRefreshTokenStore>();

        var record = CreateEmailChallenge(now);
        await first.StoreAsync(record, ct);
        (await second.UpdateOtpStateAsync(record.TokenHash, "Email", "early", "v1", 1, now.AddMinutes(2), now, ct))
            .Should().BeFalse();

        var nowAfterCooldown = now.AddSeconds(30);
        var resends = await RunConcurrentOperationsAsync(
            first,
            second,
            ct,
            (store, i) => store.UpdateOtpStateAsync(
                record.TokenHash, "Email", $"otp-{i}", "v1", 1, now.AddMinutes(2), nowAfterCooldown, ct));
        resends.Count(won => won).Should().Be(1);
        var winner = Array.IndexOf(resends, true);
        var current = await first.FindAsync(record.TokenHash, nowAfterCooldown, ct);
        current!.OtpCodeHash.Should().Be($"otp-{winner}");
        current.ResendCount.Should().Be(1);

        var attempts = await RunConcurrentOperationsAsync(
            first, second, ct, (store, _) => store.IncrementAttemptAsync(record.TokenHash, nowAfterCooldown, ct));
        attempts.Where(a => a.Status == MfaChallengeAttemptStatus.Allowed)
            .Select(a => a.Record!.AttemptCount).Order().Should().Equal(1, 2, 3, 4, 5);
        attempts.Count(a => a.Status == MfaChallengeAttemptStatus.Exhausted).Should().Be(1);
        attempts.Count(a => a.Status == MfaChallengeAttemptStatus.NotFoundOrExpired).Should().Be(10);

        var another = record with { TokenHash = "second-challenge", ChallengeId = Guid.NewGuid() };
        await first.StoreAsync(another, ct);
        var consumed = await RunConcurrentOperationsAsync(
            first, second, ct, (store, _) => store.ConsumeAsync(another.TokenHash, now, ct));
        consumed.Count(won => won).Should().Be(1);

        var replay = await RunConcurrentOperationsAsync(
            first, second, ct, (store, _) => store.TryRecordUsedOtpAsync("guard", "v1", "user-1", now, now.AddSeconds(90), ct));
        replay.Count(won => won).Should().Be(1);
        (await second.TryRecordUsedOtpAsync("guard", "v1", "user-1", now.AddSeconds(90), now.AddMinutes(3), ct))
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RevocationStoresSupportJtiSessionAndUserCutoffSemanticsAsync(bool distributed)
    {
        var time = new AdjustableTimeProvider();
        var cache = new MemoryDistributedCache(MSOptions.Create(new MemoryDistributedCacheOptions()));
        ITokenRevocationStore store = distributed
            ? new DistributedCacheTokenRevocationStore(cache, time)
            : new InMemoryTokenRevocationStore(time, MSOptions.Create(new IdentityJwtOptions()));
        var ct = TestContext.Current.CancellationToken;
        var now = time.GetUtcNow();
        const string issuer = "https://auth.example.test";

        await store.RevokeAsync(
            new TokenRevocationEntry(issuer, RevocationSubjectType.Jti, "jti-1", now, now.AddMinutes(10), null, "logout"),
            ct);
        await store.RevokeAsync(
            new TokenRevocationEntry(issuer, RevocationSubjectType.SessionId, "sid-1", now, now.AddMinutes(10), null, "logout"),
            ct);
        await store.RevokeAsync(
            new TokenRevocationEntry(issuer, RevocationSubjectType.UserId, "uid-1", now, now.AddMinutes(10), now, "revoke-all"),
            ct);

        (await store.IsRevokedAsync(new TokenRevocationCheckContext(issuer, "jti-1", "other", "other", now.AddSeconds(5)), ct))
            .Should().BeTrue();
        (await store.IsRevokedAsync(new TokenRevocationCheckContext(issuer, "other", "sid-1", "other", now.AddSeconds(5)), ct))
            .Should().BeTrue();
        (await store.IsRevokedAsync(new TokenRevocationCheckContext(issuer, "other", "other", "uid-1", now), ct))
            .Should().BeTrue();
        (await store.IsRevokedAsync(new TokenRevocationCheckContext(issuer, "other", "other", "uid-1", now.AddSeconds(1)), ct))
            .Should().BeFalse();
    }

    private static MfaChallengeRecord CreateEmailChallenge(DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            "shared-challenge",
            "v1",
            MfaChallengePurpose.LoginChallenge,
            "user-1",
            "stamp",
            "Email",
            "Email",
            "code",
            AttemptCount: 0,
            MaxAttempts: 5,
            ResendCount: 0,
            ResendAvailableAtUtc: now.AddSeconds(30),
            CreatedAtUtc: now,
            ExpiresAtUtc: now.AddMinutes(5));

    private static Task<TResult[]> RunConcurrentOperationsAsync<TResult>(
        IMfaChallengeStore first,
        IMfaChallengeStore second,
        CancellationToken cancellationToken,
        Func<IMfaChallengeStore, int, Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 16).Select(async i =>
        {
            await gate.Task.WaitAsync(cancellationToken);
            return await operation(i % 2 == 0 ? first : second, i);
        }).ToArray();
        gate.SetResult();
        return Task.WhenAll(tasks);
    }

    private static RefreshTokenRecord CreateRefreshRecord(
        string hashHex,
        string sessionId,
        string userId,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            hashHex,
            "hk-1",
            sessionId,
            userId,
            "stamp-1",
            "pwd",
            now,
            now.AddDays(7),
            now.AddDays(30),
            null,
            null,
            null,
            null);
}
