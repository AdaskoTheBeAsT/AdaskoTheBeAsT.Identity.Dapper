using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest.Fixtures;
using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest;

[Collection(SqlServerJwtCollection.Name)]
public sealed class SqlServerMfaChallengeStoreIntegrationTests(SqlServerJwtFixture fixture)
{
    [Fact]
    public async Task ParallelIncrementAttemptStrictlyEnforcesMaxAttemptsCeilingAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 11, 10, 14, 0, 0, TimeSpan.Zero);
        await using var scope = fixture.CreateStoreScope(new FakeTimeProvider(now));

        var challengeHash = SqlHashHelper.Sha256Hex($"mfa-race-{Guid.NewGuid():N}");
        var challenge = new MfaChallengeRecord(
            Guid.NewGuid(),
            challengeHash,
            "v1",
            MfaChallengePurpose.LoginChallenge,
            Guid.NewGuid().ToString("D"),
            "stamp-mfa",
            "Authenticator,Email",
            "Authenticator",
            null,
            AttemptCount: 0,
            MaxAttempts: 5,
            ResendCount: 0,
            ResendAvailableAtUtc: null,
            now,
            now.AddMinutes(5));

        await scope.MfaStore.StoreAsync(challenge, ct);

        using var barrier = new SemaphoreSlim(0, 16);
        var tasks = Enumerable.Range(0, 16).Select(async _ =>
        {
            await barrier.WaitAsync(ct);
            return await scope.MfaStore.IncrementAttemptAsync(challengeHash, now.AddSeconds(5), ct);
        }).ToArray();

        barrier.Release(16);
        var results = await Task.WhenAll(tasks);

        results.Count(r => r.Status == MfaChallengeAttemptStatus.Allowed).Should().Be(5);
        results.Count(r => r.Status == MfaChallengeAttemptStatus.Exhausted).Should().Be(1);
        results.Count(r => r.Status == MfaChallengeAttemptStatus.NotFoundOrExpired).Should().Be(10);
        results.Where(r => r.Status == MfaChallengeAttemptStatus.Allowed)
            .Select(r => r.Record!.AttemptCount)
            .Order()
            .Should()
            .Equal(1, 2, 3, 4, 5);
        (await scope.MfaStore.FindAsync(challengeHash, now.AddSeconds(5), ct)).Should().BeNull();
    }

    [Fact]
    public async Task CrossInstanceOtpReplayGuardAndConsumeEnforceSingleWinnerAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 11, 10, 15, 0, 0, TimeSpan.Zero);
        await using var podA = fixture.CreateStoreScope(new FakeTimeProvider(now));
        await using var podB = fixture.CreateStoreScope(new FakeTimeProvider(now));

        var replayHash = SqlHashHelper.Sha256Hex($"totp-replay-{Guid.NewGuid():N}");
        var userId = Guid.NewGuid().ToString("D");

        using var otpBarrier = new SemaphoreSlim(0, 16);
        var otpTasks = Enumerable.Range(0, 16).Select(async idx =>
        {
            var store = idx % 2 == 0 ? podA.MfaStore : podB.MfaStore;
            await otpBarrier.WaitAsync(ct);
            return await store.TryRecordUsedOtpAsync(replayHash, "v1", userId, now, now.AddMinutes(2), ct);
        }).ToArray();

        otpBarrier.Release(16);
        var otpWinners = await Task.WhenAll(otpTasks);
        otpWinners.Count(won => won).Should().Be(1);
        otpWinners.Count(won => !won).Should().Be(15);

        var challengeHash = SqlHashHelper.Sha256Hex($"mfa-consume-{Guid.NewGuid():N}");
        var newOtpHash = SqlHashHelper.Sha256Hex($"otp-code-{Guid.NewGuid():N}");
        await podA.MfaStore.StoreAsync(
            new MfaChallengeRecord(
                Guid.NewGuid(),
                challengeHash,
                "v1",
                MfaChallengePurpose.LoginChallenge,
                userId,
                "stamp",
                "Email",
                "Email",
                null,
                0,
                5,
                0,
                null,
                now,
                now.AddMinutes(5)),
            ct);

        (await podA.MfaStore.UpdateOtpStateAsync(challengeHash, "Email", newOtpHash, "v2", 1, now.AddSeconds(60), now, ct))
            .Should().BeTrue();

        var updated = await podB.MfaStore.FindAsync(challengeHash, now, ct);
        updated!.OtpCodeHash.Should().Be(newOtpHash);
        updated.ResendCount.Should().Be(1);

        (await podB.MfaStore.UpdateOtpStateAsync(challengeHash, "Email", newOtpHash, "v2", 2, now.AddMinutes(2), now, ct))
            .Should().BeFalse();
        var nextWindow = now.AddSeconds(60);
        await AssertSingleResendAsync(podA.MfaStore, podB.MfaStore, challengeHash, nextWindow, ct);

        var consumeResults = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(i => (i % 2 == 0 ? podA.MfaStore : podB.MfaStore).ConsumeAsync(challengeHash, nextWindow, ct)));
        consumeResults.Count(c => c).Should().Be(1);
    }

    private static async Task AssertSingleResendAsync(
        IMfaChallengeStore first,
        IMfaChallengeStore second,
        string challengeHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var resendResults = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(i => (i % 2 == 0 ? first : second).UpdateOtpStateAsync(
                challengeHash, "Email", SqlHashHelper.Sha256Hex($"resend-{i}"), "v2", 2, now.AddSeconds(60), now, cancellationToken)));
        resendResults.Count(won => won).Should().Be(1);
        var winnerHash = SqlHashHelper.Sha256Hex($"resend-{Array.IndexOf(resendResults, true)}");
        var resent = await first.FindAsync(challengeHash, now, cancellationToken);
        resent!.OtpCodeHash.Should().Be(winnerHash);
    }
}
