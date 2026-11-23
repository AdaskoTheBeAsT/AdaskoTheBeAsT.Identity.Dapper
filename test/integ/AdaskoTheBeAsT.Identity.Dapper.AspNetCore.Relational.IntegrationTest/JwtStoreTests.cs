using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Refresh;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational.IntegrationTest;

public sealed class JwtStoreTests
{
    [Theory]
    [InlineData("Sqlite")]
    [InlineData("PostgreSql")]
    [InlineData("Oracle")]
    [InlineData("MySql")]
    [InlineData("MySqlConnector")]
    public async Task ProviderPersistsJwtStateAndSerializesCompetingWrites(string provider)
    {
        if (!string.Equals(provider, "Sqlite", StringComparison.Ordinal))
        {
            Assert.SkipUnless(
                string.Equals(Environment.GetEnvironmentVariable("IDENTITY_JWT_TEST_CONTAINERS"), "1", StringComparison.Ordinal),
                "Set IDENTITY_JWT_TEST_CONTAINERS=1 to run server providers on a Linux Docker engine.");
        }

        var ct = TestContext.Current.CancellationToken;
        await using var database = await TestDatabase.StartAsync(provider, ct);
        await database.EnsureSchemaAsync(ct);
        var clock = new TestClock();
        await using var issuer = database.BuildIssuer(clock);
        await using var secondIssuer = database.BuildIssuer(clock);
        await using var validator = database.BuildValidator(clock);
        await ExerciseRefreshAsync(issuer, secondIssuer, clock.GetUtcNow(), ct);
        await ExerciseMfaAsync(issuer, secondIssuer, clock.GetUtcNow(), ct);
        await ExerciseRevocationAsync(issuer, validator, clock, ct);
        await ExerciseCatalogAsync(issuer, validator, database, ct);
    }

    [Theory]
    [InlineData("Sqlite", "SQLite")]
    [InlineData("PostgreSql", "PostgreSQL")]
    [InlineData("Oracle", "Oracle")]
    [InlineData("MySql", "MySql")]
    [InlineData("MySqlConnector", "MySql")]
    public async Task DefaultMigrationScriptMatchesSchemaHelper(string provider, string dialect)
    {
        var ct = TestContext.Current.CancellationToken;
        using var stream = typeof(JwtStoreTests).Assembly.GetManifestResourceStream($"{dialect}.IdentityJwt.sql");
        using var reader = new StreamReader(stream!);
        var sql = (await reader.ReadToEndAsync(ct)).ReplaceLineEndings("\n").Trim();
        var oracle = string.Equals(dialect, "Oracle", StringComparison.Ordinal);
        var expected = TestDatabase.BuildStatements(provider)
            .Select(statement => statement.ReplaceLineEndings("\n") +
                (oracle && statement.StartsWith("BEGIN", StringComparison.Ordinal) ? "\n/" : ";"));
        Assert.Equal(
            "-- Identity JWT runtime tables and default claim kinds (default provider options).\n\n" +
            string.Join("\n\n", expected) + (oracle ? "\n\nCOMMIT;" : string.Empty),
            sql);
    }

    private static async Task ExerciseRefreshAsync(
        IServiceProvider issuer,
        IServiceProvider secondIssuer,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var store = issuer.GetRequiredService<IRefreshTokenStore>();
        var other = secondIssuer.GetRequiredService<IRefreshTokenStore>();
        var first = new RefreshTokenRecord(Guid.NewGuid(), "first", "key", "session", "user", "stamp", "pwd,mfa", now, now.AddMinutes(10), now.AddHours(1));
        await store.StoreAsync(first, ct);
        Assert.Equal(first, await other.FindByHashAsync(first.TokenHash, ct));
        var left = first with { Id = Guid.NewGuid(), TokenHash = "left" };
        var right = first with { Id = Guid.NewGuid(), TokenHash = "right" };
        var rotations = await RaceAsync(
            () => store.RotateAsync(first.TokenHash, left, now, ct),
            () => other.RotateAsync(first.TokenHash, right, now, ct),
            ct);
        var rotated = Assert.Single(rotations, result => result.Status == RefreshTokenRotationStatus.Rotated);
        Assert.Single(rotations, result => result.Status == RefreshTokenRotationStatus.Replayed);
        Assert.Equal("refresh_token_replay", (await other.FindByHashAsync(rotated.ReplacementRecord!.TokenHash, ct))?.RevocationReason);

        await store.StoreAsync(first with { TokenHash = "second", SessionId = "other-session" }, ct);
        Assert.Equal(new[] { "other-session" }, await other.RevokeAllUserSessionsAsync("user", now, "logout-all", ct));
        Assert.Equal("logout-all", (await store.FindByHashAsync("second", ct))?.RevocationReason);
        await store.StoreAsync(first with { TokenHash = "expired-1", ExpiresAtUtc = now }, ct);
        await store.StoreAsync(first with { TokenHash = "expired-2", SessionExpiresAtUtc = now }, ct);
        Assert.Equal(1, await store.PruneExpiredAsync(1, ct));
        Assert.Equal(1, await other.PruneExpiredAsync(1, ct));
        Assert.Equal(0, await store.PruneExpiredAsync(1, ct));
    }

    private static async Task ExerciseMfaAsync(
        IServiceProvider issuer,
        IServiceProvider secondIssuer,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var store = issuer.GetRequiredService<IMfaChallengeStore>();
        var other = secondIssuer.GetRequiredService<IMfaChallengeStore>();
        var challenge = new MfaChallengeRecord(
            Guid.NewGuid(),
            "challenge",
            "key",
            MfaChallengePurpose.LoginChallenge,
            "user",
            null,
            "email",
            null,
            null,
            0,
            3,
            0,
            null,
            now,
            now.AddMinutes(10));
        await store.StoreAsync(challenge, ct);
        var attempts = await RaceAsync(
            () => store.IncrementAttemptAsync(challenge.TokenHash, now, ct),
            () => other.IncrementAttemptAsync(challenge.TokenHash, now, ct),
            ct);
        Assert.All(attempts, attempt => Assert.Equal(MfaChallengeAttemptStatus.Allowed, attempt.Status));
        Assert.Equal(2, (await store.FindAsync(challenge.TokenHash, now, ct))?.AttemptCount);
        var resends = await RaceAsync(
            () => store.UpdateOtpStateAsync(challenge.TokenHash, "email", "hash-1", "key", 1, now.AddMinutes(1), now, ct),
            () => other.UpdateOtpStateAsync(challenge.TokenHash, "email", "hash-2", "key", 1, now.AddMinutes(1), now, ct),
            ct);
        Assert.Single(resends, value => value);
        var consumes = await RaceAsync(
            () => store.ConsumeAsync(challenge.TokenHash, now, ct),
            () => other.ConsumeAsync(challenge.TokenHash, now, ct),
            ct);
        Assert.Single(consumes, value => value);

        var replay = await RaceAsync(
            () => store.TryRecordUsedOtpAsync("otp", "key", "user", now, now.AddMinutes(1), ct),
            () => other.TryRecordUsedOtpAsync("otp", "key", "user", now, now.AddMinutes(1), ct),
            ct);
        Assert.Single(replay, value => value);
        await store.StoreAsync(challenge with { TokenHash = "expired", ExpiresAtUtc = now }, ct);
        Assert.Null(await other.FindAsync("expired", now, ct));
        Assert.Equal(1, await other.PruneExpiredAsync(1, ct));
        Assert.Equal(0, await store.PruneExpiredAsync(1, ct));
    }

    private static async Task ExerciseRevocationAsync(
        IServiceProvider issuer,
        IServiceProvider validator,
        TestClock clock,
        CancellationToken ct)
    {
        var store = issuer.GetRequiredService<ITokenRevocationStore>();
        var other = validator.GetRequiredService<ITokenRevocationStore>();
        var now = clock.GetUtcNow();
        var entry = new TokenRevocationEntry("test-issuer", RevocationSubjectType.Jti, "token", now, now.AddMinutes(1), Reason: "logout");
        await store.RevokeAsync(entry, ct);
        await store.RevokeAsync(entry with { SubjectType = RevocationSubjectType.SessionId, SubjectKey = "session" }, ct);
        await store.RevokeAsync(
            entry with
            {
                SubjectType = RevocationSubjectType.UserId,
                SubjectKey = "user",
                RevokedBeforeUtc = now,
                ExpiresAtUtc = now.AddMinutes(3),
            },
            ct);
        await store.RevokeAsync(
            entry with
            {
                SubjectType = RevocationSubjectType.UserId,
                SubjectKey = "user",
                RevokedBeforeUtc = now.AddMinutes(-1),
            },
            ct);
        Assert.True(await other.IsRevokedAsync(new("test-issuer", "token", null, null, now), ct));
        Assert.True(await other.IsRevokedAsync(new("test-issuer", null, "session", null, now), ct));
        Assert.False(await other.IsRevokedAsync(new("another", "token", "session", "user", now), ct));
        Assert.False(await other.IsRevokedAsync(new("test-issuer", null, null, "user", now.AddTicks(1)), ct));
        Assert.True(await other.IsRevokedAsync(new("test-issuer", null, null, "user", null), ct));

        clock.Now = now.AddMinutes(1);
        Assert.False(await other.IsRevokedAsync(new("test-issuer", "token", "session", null, now), ct));
        Assert.True(await other.IsRevokedAsync(new("test-issuer", null, null, "user", now), ct));
        Assert.Equal(1, await store.PruneExpiredAsync(1, ct));
        Assert.Equal(1, await other.PruneExpiredAsync(1, ct));
        Assert.Equal(0, await store.PruneExpiredAsync(1, ct));
    }

    private static async Task ExerciseCatalogAsync(
        IServiceProvider issuer,
        IServiceProvider validator,
        TestDatabase database,
        CancellationToken ct)
    {
        var catalog = issuer.GetRequiredService<IJwtClaimCatalogStore>();
        Assert.Equal(3, (await catalog.GetClaimKindsAsync(false, ct)).Count);
        var frontend = await catalog.UpsertClaimKindAsync(new(IdentityClaimKindNames.FrontendClaim, "custom", true), ct);
        await database.EnsureSchemaAsync(ct);
        Assert.Contains(frontend, await catalog.GetClaimKindsAsync(false, ct));
        var first = await catalog.UpsertClaimDefinitionAsync(new(frontend.Name, "sample"), ct);
        await catalog.UpsertClaimDefinitionAsync(new(IdentityClaimKindNames.BackendClaim, "permission"), ct);
        var filtered = await catalog.FilterAccessTokenClaimsAsync(
            [new("sample", "1"), new("sample", "1"), new("permission", "read"), new("unknown", "3")],
            null,
            ct);
        Assert.Equal("sample", Assert.Single(filtered).Type);

        await database.SeedIdentityClaimsAsync(ct);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new("sub", database.UserId)], "test"));
        var transformation = validator.GetRequiredService<IClaimsTransformation>();
        var enriched = await transformation.TransformAsync(principal);
        Assert.Equal("1", Assert.Single(enriched.FindAll("sample")).Value);
        Assert.Equal("read", Assert.Single(enriched.FindAll("permission")).Value);
        Assert.Same(enriched, await transformation.TransformAsync(enriched));

        var inactive = await catalog.UpsertClaimDefinitionAsync(new(frontend.Name, "SAMPLE", IsActive: false), ct);
        Assert.Equal(first.Id, inactive.Id);
        Assert.Empty(await catalog.GetClaimDefinitionsAsync(frontend.Name, false, ct));
        Assert.Single(await catalog.GetClaimDefinitionsAsync(frontend.Name, true, ct));
    }

    private static Task<T[]> RaceAsync<T>(Func<Task<T>> first, Func<Task<T>> second, CancellationToken ct)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var left = Task.Run(() => RunAsync(first), ct);
        var right = Task.Run(() => RunAsync(second), ct);
        start.SetResult();
        return Task.WhenAll(left, right);

        async Task<T> RunAsync(Func<Task<T>> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            await start.Task.WaitAsync(ct);
            return await action();
        }
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
