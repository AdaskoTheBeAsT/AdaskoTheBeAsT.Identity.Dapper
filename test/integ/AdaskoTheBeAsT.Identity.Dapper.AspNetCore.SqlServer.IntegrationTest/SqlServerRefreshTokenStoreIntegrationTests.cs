using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest.Fixtures;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest;

[Collection(SqlServerJwtCollection.Name)]
public sealed class SqlServerRefreshTokenStoreIntegrationTests(SqlServerJwtFixture fixture)
{
    [Fact]
    public async Task ParallelRotateContentionProducesExactlyOneRotatedAndFifteenReplayedAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
        await using var scope = fixture.CreateStoreScope(new FakeTimeProvider(now));

        var initialHash = SqlHashHelper.Sha256Hex($"rt-race-{Guid.NewGuid():N}");
        var sessionId = $"sid-{Guid.NewGuid():N}";
        var userId = Guid.NewGuid().ToString("D");

        await scope.RefreshStore.StoreAsync(
            BuildRecord(initialHash, sessionId, userId, now),
            ct);

        var replacements = Enumerable.Range(0, 16)
            .Select(i => BuildRecord(SqlHashHelper.Sha256Hex($"rt-next-{sessionId}-{i}"), sessionId, userId, now))
            .ToArray();

        using var barrier = new SemaphoreSlim(0, 16);
        var tasks = replacements.Select(async rep =>
        {
            await barrier.WaitAsync(ct);
            return await scope.RefreshStore.RotateAsync(initialHash, rep, now.AddMinutes(1), ct);
        }).ToArray();

        barrier.Release(16);
        var results = await Task.WhenAll(tasks);

        results.Count(r => r.Status == RefreshTokenRotationStatus.Rotated).Should().Be(1);
        results.Count(r => r.Status == RefreshTokenRotationStatus.Replayed).Should().Be(15);

        var winner = results.Single(r => r.Status == RefreshTokenRotationStatus.Rotated);
        var consumedOriginal = await scope.RefreshStore.FindByHashAsync(initialHash, ct);
        consumedOriginal.Should().NotBeNull();
        consumedOriginal.ConsumedAtUtc.Should().Be(now.AddMinutes(1));
        consumedOriginal.ReplacedByTokenHash.Should().NotBeNullOrWhiteSpace();

        var activeReplacement = await scope.RefreshStore.FindByHashAsync(consumedOriginal.ReplacedByTokenHash, ct);
        activeReplacement.Should().NotBeNull();
        activeReplacement.ConsumedAtUtc.Should().BeNull();
        _ = winner;
    }

    [Fact]
    public async Task RotateHandlesRevokedExpiredAndSessionUserScopedRevocationAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 11, 10, 13, 0, 0, TimeSpan.Zero);
        await using var scope = fixture.CreateStoreScope(new FakeTimeProvider(now));

        var userId = Guid.NewGuid().ToString("D");
        var sessionA = $"sid-a-{Guid.NewGuid():N}";
        var sessionB = $"sid-b-{Guid.NewGuid():N}";
        var hashA = SqlHashHelper.Sha256Hex($"hash-a-{Guid.NewGuid():N}");
        var hashB = SqlHashHelper.Sha256Hex($"hash-b-{Guid.NewGuid():N}");

        await scope.RefreshStore.StoreAsync(BuildRecord(hashA, sessionA, userId, now), ct);
        await scope.RefreshStore.StoreAsync(BuildRecord(hashB, sessionB, userId, now), ct);

        (await scope.RefreshStore.RevokeSessionAsync(sessionA, now, "logout", ct)).Should().Be(1);

        var rotateRevoked = await scope.RefreshStore.RotateAsync(
            hashA,
            BuildRecord(SqlHashHelper.Sha256Hex($"rep-{Guid.NewGuid():N}"), sessionA, userId, now),
            now,
            ct);
        rotateRevoked.Status.Should().Be(RefreshTokenRotationStatus.Revoked);

        var unknownRotate = await scope.RefreshStore.RotateAsync(
            SqlHashHelper.Sha256Hex($"missing-{Guid.NewGuid():N}"),
            BuildRecord(SqlHashHelper.Sha256Hex($"rep2-{Guid.NewGuid():N}"), sessionB, userId, now),
            now,
            ct);
        unknownRotate.Status.Should().Be(RefreshTokenRotationStatus.NotFoundOrExpired);

        (await scope.RefreshStore.RevokeAllUserSessionsAsync(userId, now, "compromised", ct)).Should().Equal(sessionB);
        var foundB = await scope.RefreshStore.FindByHashAsync(hashB, ct);
        foundB.Should().NotBeNull();
        foundB.RevokedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task CustomSchemaAndOptionsValidatorEnforceSafeSqlIdentifiersAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow;
        await using var customScope = fixture.CreateStoreScope(new FakeTimeProvider(now), SqlServerJwtFixture.CustomSchemaName);

        var hash = SqlHashHelper.Sha256Hex($"custom-schema-{Guid.NewGuid():N}");
        await customScope.RefreshStore.StoreAsync(BuildRecord(hash, "sid-custom", "u-custom", now), ct);
        (await customScope.RefreshStore.FindByHashAsync(hash, ct)).Should().NotBeNull();

        var validator = new IdentityJwtSqlServerOptionsValidator();
        var injectionAttempt = new IdentityJwtSqlServerOptions
        {
            ConnectionString = fixture.ConnectionString,
            SchemaName = "dbo]; DROP TABLE Users; --",
        };
        validator.Validate(null, injectionAttempt).Failed.Should().BeTrue();

        var factoryConfigured = new IdentityJwtSqlServerOptions
        {
            ConnectionFactory = _ => new SqlConnection(fixture.ConnectionString),
            SchemaName = SqlServerJwtFixture.CustomSchemaName,
        };
        validator.Validate(null, factoryConfigured).Succeeded.Should().BeTrue();
    }

    private static RefreshTokenRecord BuildRecord(
        string hash,
        string sessionId,
        string userId,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            hash,
            "v1",
            sessionId,
            userId,
            "stamp-1",
            "pwd,mfa,otp",
            now,
            now.AddDays(7),
            now.AddDays(30));
}
