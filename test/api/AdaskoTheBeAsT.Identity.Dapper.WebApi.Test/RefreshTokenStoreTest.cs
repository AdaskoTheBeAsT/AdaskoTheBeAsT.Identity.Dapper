using System.Collections;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Test;

public sealed class RefreshTokenStoreTest
{
    private static readonly string[] RefreshTokenProperties = { "AudienceId", "ExpiresUtc", "SecurityStamp", "Subject" };

    [Fact]
    public void DefaultCapacityIsTenThousand()
    {
        new TokenServiceOptions().RefreshTokenCapacity.Should().Be(10000);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void CapacityMustBePositive(int capacity)
    {
        var options = new TokenServiceOptions { RefreshTokenCapacity = capacity };
        FluentActions.Invoking(() => new TokenService(options, TimeProvider.System)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConstructorRejectsMissingOptionsOrClock()
    {
        FluentActions.Invoking(() => new TokenService(null!, TimeProvider.System)).Should().ThrowExactly<ArgumentNullException>();
        FluentActions.Invoking(() => new TokenService(new TokenServiceOptions(), null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void StoreNeverExceedsCapacityAndEvictsOldestOutstandingIssuance()
    {
        const int capacity = 3;
        using var tokens = CreateTokens(capacity);
        var issued = new List<Token>();
        for (var i = 0; i < 20; i++)
        {
            issued.Add(Generate(tokens));
            tokens.OutstandingRefreshTokenCount.Should().Be(Math.Min(i + 1, capacity));
            AssertIndexesHaveCount(tokens, Math.Min(i + 1, capacity));
        }

        foreach (var evicted in issued.Take(issued.Count - capacity))
        {
            FluentActions.Invoking(() => tokens.ConsumeRefreshToken(evicted.RefreshToken!)).Should().ThrowExactly<InvalidRefreshTokenException>();
        }

        foreach (var retained in issued.TakeLast(capacity))
        {
            tokens.ConsumeRefreshToken(retained.RefreshToken!).Should().NotBeNull();
        }

        tokens.OutstandingRefreshTokenCount.Should().Be(0);
        AssertIndexesHaveCount(tokens, 0);
    }

    [Fact]
    public void ConsumingTokenImmediatelyReleasesSlotWithoutEvictingOtherTokens()
    {
        using var tokens = CreateTokens(2);
        var first = Generate(tokens);
        var second = Generate(tokens);
        tokens.ConsumeRefreshToken(second.RefreshToken!);
        tokens.OutstandingRefreshTokenCount.Should().Be(1);
        AssertIndexesHaveCount(tokens, 1);

        var replacement = Generate(tokens);
        tokens.OutstandingRefreshTokenCount.Should().Be(2);
        tokens.ConsumeRefreshToken(first.RefreshToken!);
        tokens.ConsumeRefreshToken(replacement.RefreshToken!);
        FluentActions.Invoking(() => tokens.ConsumeRefreshToken(second.RefreshToken!)).Should().ThrowExactly<InvalidRefreshTokenException>();
        AssertIndexesHaveCount(tokens, 0);
    }

    [Fact]
    public void SingleSlotSupportsRefreshRotationAndReplayRejection()
    {
        using var tokens = CreateTokens(1);
        var user = CreateUser();
        var original = Generate(tokens, user);
        var metadata = tokens.ConsumeRefreshToken(original.RefreshToken!);
        metadata.Subject.Should().Be(user.Id.ToString("D"));
        metadata.SecurityStamp.Should().Be(user.SecurityStamp);
        var refreshed = Generate(tokens, user);

        refreshed.RefreshToken.Should().NotBe(original.RefreshToken);
        FluentActions.Invoking(() => tokens.ConsumeRefreshToken(original.RefreshToken!)).Should().ThrowExactly<InvalidRefreshTokenException>();
        tokens.ConsumeRefreshToken(refreshed.RefreshToken).Should().NotBeNull();
        tokens.OutstandingRefreshTokenCount.Should().Be(0);
    }

    [Fact]
    public void ExpiryRejectsTokenAtExactBoundaryAndReleasesSlot()
    {
        var clock = new ManualTimeProvider();
        using var tokens = CreateTokens(1, clock);
        var original = Generate(tokens);
        clock.Advance(TimeSpan.FromSeconds(original.ExpiresIn - 1));
        tokens.OutstandingRefreshTokenCount.Should().Be(1);
        clock.Advance(TimeSpan.FromSeconds(1));
        FluentActions.Invoking(() => tokens.ConsumeRefreshToken(original.RefreshToken!)).Should().ThrowExactly<InvalidRefreshTokenException>();
        tokens.OutstandingRefreshTokenCount.Should().Be(0);
        AssertIndexesHaveCount(tokens, 0);

        var replacement = Generate(tokens);
        tokens.ConsumeRefreshToken(replacement.RefreshToken!).Should().NotBeNull();
    }

    [Fact]
    public void UnexpiredTokenIsConsumableImmediatelyBeforeBoundary()
    {
        var clock = new ManualTimeProvider();
        using var tokens = CreateTokens(1, clock);
        var original = Generate(tokens);
        clock.Advance(TimeSpan.FromSeconds(original.ExpiresIn).Subtract(TimeSpan.FromTicks(1)));
        tokens.ConsumeRefreshToken(original.RefreshToken!).Should().NotBeNull();
    }

    [Fact]
    public void GenerationPrunesExpiredEntriesBeforeAdmittingNewToken()
    {
        var clock = new ManualTimeProvider();
        using var tokens = CreateTokens(2, clock);
        var expired = Generate(tokens);
        clock.Advance(TimeSpan.FromSeconds(1));
        var survivor = Generate(tokens);
        clock.Advance(TimeSpan.FromSeconds(expired.ExpiresIn - 1));
        var replacement = Generate(tokens);

        tokens.OutstandingRefreshTokenCount.Should().Be(2);
        FluentActions.Invoking(() => tokens.ConsumeRefreshToken(expired.RefreshToken!)).Should().ThrowExactly<InvalidRefreshTokenException>();
        tokens.ConsumeRefreshToken(survivor.RefreshToken!).Should().NotBeNull();
        tokens.ConsumeRefreshToken(replacement.RefreshToken!).Should().NotBeNull();
        AssertIndexesHaveCount(tokens, 0);
    }

    [Fact]
    public void ExpiryPrunesAllEntriesEvenWhenClockMovesBackwardsBetweenIssuances()
    {
        var clock = new ManualTimeProvider();
        using var tokens = CreateTokens(2, clock);
        var laterExpiry = Generate(tokens);
        clock.Advance(TimeSpan.FromMinutes(-10));
        var earlierExpiry = Generate(tokens);
        clock.Advance(TimeSpan.FromHours(1));

        tokens.OutstandingRefreshTokenCount.Should().Be(1);
        FluentActions.Invoking(() => tokens.ConsumeRefreshToken(earlierExpiry.RefreshToken!)).Should().ThrowExactly<InvalidRefreshTokenException>();
        tokens.ConsumeRefreshToken(laterExpiry.RefreshToken!).Should().NotBeNull();
        AssertIndexesHaveCount(tokens, 0);
    }

    [Fact]
    public async Task ConcurrentConsumeHasExactlyOneWinnerAndReleasesSlot()
    {
        using var tokens = CreateTokens(1);
        var original = Generate(tokens);
        var outcomes = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(
        () =>
        {
            try
            {
                tokens.ConsumeRefreshToken(original.RefreshToken!);
                return true;
            }
            catch (InvalidRefreshTokenException)
            {
                return false;
            }
        },
        TestContext.Current.CancellationToken)));

        outcomes.Should().ContainSingle(succeeded => succeeded);
        tokens.OutstandingRefreshTokenCount.Should().Be(0);
        var replacement = Generate(tokens);
        tokens.ConsumeRefreshToken(replacement.RefreshToken!).Should().NotBeNull();
        AssertIndexesHaveCount(tokens, 0);
    }

    [Fact]
    public async Task ConcurrentIssuanceMaintainsStrictBoundAndAllRetainedTokensAreConsumable()
    {
        const int capacity = 4;
        using var tokens = CreateTokens(capacity);
        var issued = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(
        () =>
        {
            var token = Generate(tokens);
            tokens.OutstandingRefreshTokenCount.Should().BeInRange(1, capacity);
            return token;
        },
        TestContext.Current.CancellationToken)));

        tokens.OutstandingRefreshTokenCount.Should().Be(capacity);
        AssertIndexesHaveCount(tokens, capacity);
        var successes = 0;
        foreach (var token in issued)
        {
#pragma warning disable CC0004 // Catch block cannot be empty
            try
            {
                tokens.ConsumeRefreshToken(token.RefreshToken!);
                successes++;
            }
            catch (InvalidRefreshTokenException)
            {
                // Tokens issued before the final capacity-sized window were evicted.
            }
#pragma warning restore CC0004 // Catch block cannot be empty
        }

        successes.Should().Be(capacity);
        tokens.OutstandingRefreshTokenCount.Should().Be(0);
        AssertIndexesHaveCount(tokens, 0);
    }

    [Fact]
    public void OptionsAreSnapshottedAtConstruction()
    {
        var options = CreateOptions(1);
        using var tokens = new TokenService(options, new ManualTimeProvider());
        options.RefreshTokenCapacity = 0;
        options.SigningKey = null;
        var first = Generate(tokens);
        var second = Generate(tokens);
        tokens.OutstandingRefreshTokenCount.Should().Be(1);
        FluentActions.Invoking(() => tokens.ConsumeRefreshToken(first.RefreshToken!)).Should().ThrowExactly<InvalidRefreshTokenException>();
        tokens.ConsumeRefreshToken(second.RefreshToken!).Should().NotBeNull();
    }

    [Fact]
    public void StoreRetainsOnlyValidationMetadataAndNotAccessTokenOrClaims()
    {
        using var tokens = CreateTokens(1);
        var user = CreateUser();
        var issued = tokens.GenerateToken(
user,
new List<string>(),
new List<Claim> { new("large-claim", new string('x', 32768)) });
        (issued.AccessToken!.Length > 32768).Should().BeTrue();

        // Inspect outstanding metadata, not just the already-consumed return value.
        var dictionary = (IDictionary)GetField(tokens, "_refreshTokens");
        var node = dictionary.Values.Cast<object>().Should().ContainSingle().Which;
#pragma warning disable REFL009 // Inspect private runtime types without adding production test hooks.
        var entry = node.GetType().GetProperty("Value")!.GetValue(node)!;
        var metadata = entry.GetType().GetProperty("Token")!.GetValue(entry).Should().BeOfType<RefreshToken>().Which;
#pragma warning restore REFL009
        metadata.Subject.Should().Be(user.Id.ToString("D"));
        metadata.SecurityStamp.Should().Be(user.SecurityStamp);
        metadata.AudienceId.Should().Be("IdentityWebApi");
        typeof(RefreshToken).GetProperties().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).Should().Equal(RefreshTokenProperties);
        typeof(TokenService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Should().NotContain(field => field.FieldType == typeof(Token) || field.FieldType == typeof(Claim));

        tokens.ConsumeRefreshToken(issued.RefreshToken!).Should().BeSameAs(metadata);
        AssertIndexesHaveCount(tokens, 0);
    }

    [Fact]
    public void StoresAreIsolatedAndSharedFrameworkCacheRemainsUnsized()
    {
        using var frameworkCache = new MemoryCache(new MemoryCacheOptions());
        frameworkCache.Set("framework-entry", "retained");
        using var first = CreateTokens(1);
        using var second = CreateTokens(1);
        var firstToken = Generate(first);
        var secondToken = Generate(second);
        Generate(second);

        first.ConsumeRefreshToken(firstToken.RefreshToken!).Should().NotBeNull();
        FluentActions.Invoking(() => second.ConsumeRefreshToken(secondToken.RefreshToken!)).Should().ThrowExactly<InvalidRefreshTokenException>();
        frameworkCache.Set("another-unsized-entry", "also-retained");
        frameworkCache.Get<string>("framework-entry").Should().Be("retained");
        frameworkCache.Get<string>("another-unsized-entry").Should().Be("also-retained");
    }

    [Fact]
    public void DisposalClearsAllIndexesAndRejectsFurtherUse()
    {
        using var tokens = CreateTokens(2);
        var original = Generate(tokens);
#pragma warning disable IDISP016 // Assert repeated disposal is harmless and further use is rejected.
        tokens.Dispose();
        tokens.Dispose();
        AssertIndexesHaveCount(tokens, 0);
        FluentActions.Invoking(() => tokens.ConsumeRefreshToken(original.RefreshToken!)).Should().ThrowExactly<ObjectDisposedException>();
        FluentActions.Invoking(() => Generate(tokens)).Should().ThrowExactly<ObjectDisposedException>();
        FluentActions.Invoking(() => tokens.OutstandingRefreshTokenCount).Should().ThrowExactly<ObjectDisposedException>();
#pragma warning restore IDISP016
    }

    private static TokenService CreateTokens(int capacity, TimeProvider? clock = null) =>
                new(CreateOptions(capacity), clock ?? new ManualTimeProvider());

    private static TokenServiceOptions CreateOptions(int capacity) => new()
    {
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
        RefreshTokenCapacity = capacity,
    };

    private static ApplicationUser CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        UserName = "refresh-user",
        SecurityStamp = "security-stamp",
    };

    private static Token Generate(TokenService tokens, ApplicationUser? user = null) =>
                tokens.GenerateToken(user ?? CreateUser(), new List<string>(), new List<Claim>());

    private static void AssertIndexesHaveCount(TokenService tokens, int expected)
    {
        foreach (var name in new[] { "_refreshTokens", "_issuanceOrder", "_expirationOrder" })
        {
            var collection = (IEnumerable)GetField(tokens, name);
            collection.Cast<object>().Should().HaveCount(expected);
        }
    }

    private static object GetField(TokenService tokens, string name) =>
                typeof(TokenService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tokens)!;

    private sealed class ManualTimeProvider : TimeProvider
    {
#pragma warning disable CC0121 // Complex fields must be readonly
        private DateTimeOffset _utcNow = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

#pragma warning restore CC0121 // Complex fields must be readonly

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan elapsed) => _utcNow += elapsed;
    }
}
