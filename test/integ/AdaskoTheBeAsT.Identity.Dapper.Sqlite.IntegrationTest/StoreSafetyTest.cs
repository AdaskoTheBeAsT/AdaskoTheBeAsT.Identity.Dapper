using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.TestCollections;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest;

public sealed class StoreSafetyTest : IClassFixture<DatabaseWithGuidIdFixture>
{
    private static readonly string[] SingleRecoveryCode = { "alpha-123" };
    private static readonly string[] ConcurrentRecoveryCodes = { "first", "second" };
    private readonly ConnectionProvider _provider;

    public StoreSafetyTest(DatabaseWithGuidIdFixture fixture) =>
            _provider = new ConnectionProvider(fixture.ConnectionString);

    [Theory]
    [InlineData("alpha-123")]
    [InlineData("ALPHA-123")]
    public async Task RecoveryCodeCanOnlyBeRedeemedOnceAsync(string submitted)
    {
        using var store = new ApplicationUserOnlyStore(_provider);
        var user = await CreateUserAsync(store);
        await store.ReplaceCodesAsync(user, SingleRecoveryCode, CancellationToken.None);
        (await store.RedeemCodeAsync(user, submitted, CancellationToken.None)).Should().BeTrue();
        (await store.RedeemCodeAsync(user, submitted, CancellationToken.None)).Should().BeFalse();
        (await store.CountCodesAsync(user, CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task MissingOrExhaustedRecoveryCodesRejectEmptyInputAsync()
    {
        using var store = new ApplicationUserOnlyStore(_provider);
        var user = await CreateUserAsync(store);
        (await store.RedeemCodeAsync(user, string.Empty, CancellationToken.None)).Should().BeFalse();
        await store.ReplaceCodesAsync(user, Array.Empty<string>(), CancellationToken.None);
        (await store.RedeemCodeAsync(user, string.Empty, CancellationToken.None)).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentRecoveryDoesNotReuseOrRestoreCodesAsync(bool differentCodes)
    {
        using var store = new ApplicationUserOnlyStore(_provider);
        var user = await CreateUserAsync(store);
        await store.ReplaceCodesAsync(user, ConcurrentRecoveryCodes, CancellationToken.None);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrived = 0;
        Task BarrierAsync()
        {
            if (Interlocked.Increment(ref arrived) == 2)
            {
                ready.SetResult();
            }

#pragma warning disable IDISP013 // Both barrier callbacks complete in the awaited WhenAll before the stores leave scope.
            return ready.Task.WaitAsync(TimeSpan.FromSeconds(15), Xunit.TestContext.Current.CancellationToken);
#pragma warning restore IDISP013
        }

        using var first = new CoordinatedStore(_provider, BarrierAsync);
        using var second = new CoordinatedStore(_provider, BarrierAsync);
        var outcomes = await Task.WhenAll(
            first.RedeemCodeAsync(user, nameof(first), CancellationToken.None),
            second.RedeemCodeAsync(user, differentCodes ? nameof(second) : nameof(first), CancellationToken.None));
        outcomes.Count(success => success).Should().Be(differentCodes ? 2 : 1);
        (await store.RedeemCodeAsync(user, nameof(first), CancellationToken.None)).Should().BeFalse();
        (await store.CountCodesAsync(user, CancellationToken.None)).Should().Be(differentCodes ? 0 : 1);
    }

    [Fact]
    public async Task StaleUserUpdateAndDeleteFailWithoutChangingCurrentDataAsync()
    {
        using var store = new ApplicationUserOnlyStore(_provider);
        var user = await CreateUserAsync(store);
        var stale = (await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None))!;
        var stamp = user.ConcurrencyStamp;
        user.PhoneNumber = "newer";
        (await store.UpdateAsync(user, CancellationToken.None)).Succeeded.Should().BeTrue();
        user.ConcurrencyStamp.Should().NotBe(stamp);
        stale.PhoneNumber = nameof(stale);
        AssertConcurrencyFailure(await store.UpdateAsync(stale, CancellationToken.None));
        AssertConcurrencyFailure(await store.DeleteAsync(stale, CancellationToken.None));
        (await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None))!.PhoneNumber.Should().Be("newer");
        (await store.DeleteAsync(user, CancellationToken.None)).Succeeded.Should().BeTrue();
        AssertConcurrencyFailure(await store.UpdateAsync(user, CancellationToken.None));
        AssertConcurrencyFailure(await store.DeleteAsync(user, CancellationToken.None));
    }

    [Fact]
    public async Task StaleRoleUpdateAndDeleteFailAsync()
    {
        using var store = new ApplicationRoleStore(_provider);
        var role = new ApplicationRole { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString() };
        (await store.CreateAsync(role, CancellationToken.None)).Succeeded.Should().BeTrue();
        var stale = (await store.FindByIdAsync(role.Id.ToString(), CancellationToken.None))!;
        var stamp = role.ConcurrencyStamp;
        role.Name = Guid.NewGuid().ToString();
        (await store.UpdateAsync(role, CancellationToken.None)).Succeeded.Should().BeTrue();
        role.ConcurrencyStamp.Should().NotBe(stamp);
        AssertConcurrencyFailure(await store.UpdateAsync(stale, CancellationToken.None));
        AssertConcurrencyFailure(await store.DeleteAsync(stale, CancellationToken.None));
        (await store.DeleteAsync(role, CancellationToken.None)).Succeeded.Should().BeTrue();
        AssertConcurrencyFailure(await store.UpdateAsync(role, CancellationToken.None));
    }

    [Fact]
    public async Task EnumerationReturnsEachUserRegardlessOfRoleCountAsync()
    {
        using var users = new ApplicationUserStore(_provider);
        using var userOnly = new ApplicationUserOnlyStore(_provider);
        using var roles = new ApplicationRoleStore(_provider);
        var none = await CreateUserAsync(userOnly);
        var one = await CreateUserAsync(userOnly);
        var two = await CreateUserAsync(userOnly);
        for (var i = 0; i < 2; i++)
        {
            var role = new ApplicationRole { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString() };
            (await roles.CreateAsync(role, CancellationToken.None)).Succeeded.Should().BeTrue();
            await users.AddToRoleAsync(two, role.Name, CancellationToken.None);
            if (i == 0)
            {
                await users.AddToRoleAsync(one, role.Name, CancellationToken.None);
            }
        }

        foreach (var id in new[] { none.Id, one.Id, two.Id })
        {
            users.Users.Where(u => u.Id == id).Should().ContainSingle();
            userOnly.Users.Where(u => u.Id == id).Should().ContainSingle();
        }
    }

    [Fact]
    public async Task CustomMappedAndNullablePropertiesRoundTripAsync()
    {
        using var store = new ApplicationUserOnlyStore(_provider);
        var user = await CreateUserAsync(store);
        user.Active = true;
        user.DisplayLabel = "custom value";
        user.IgnoredProperty = "not persisted";
        (await store.UpdateAsync(user, CancellationToken.None)).Succeeded.Should().BeTrue();
        var loaded = (await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None))!;
        loaded.Active.Should().BeTrue();
        loaded.DisplayLabel.Should().Be("custom value");
        loaded.IgnoredProperty.Should().BeNull();
        loaded.Active = false;
        loaded.DisplayLabel = null;
        (await store.UpdateAsync(loaded, CancellationToken.None)).Succeeded.Should().BeTrue();
        var updated = (await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None))!;
        updated.Active.Should().BeFalse();
        updated.DisplayLabel.Should().BeNull();
    }

    [Fact]
    public async Task ExistingNullTokenCanBeReplacedAsync()
    {
        using var store = new ApplicationUserOnlyStore(_provider);
        var user = await CreateUserAsync(store);
        await store.SetTokenAsync(user, "test", "nullable", value: null, CancellationToken.None);
        await store.SetTokenAsync(user, "test", "nullable", "replacement", CancellationToken.None);
        (await store.GetTokenAsync(user, "test", "nullable", CancellationToken.None)).Should().Be("replacement");
    }

    private static void AssertConcurrencyFailure(IdentityResult result)
    {
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ConcurrencyFailure");
    }

    private static async Task<ApplicationUser> CreateUserAsync(ApplicationUserOnlyStore store)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        (await store.CreateAsync(user, CancellationToken.None)).Succeeded.Should().BeTrue();
        return user;
    }

    private sealed class ConnectionProvider(string connectionString) : IIdentityDbConnectionProvider<SqliteConnection>
    {
        public SqliteConnection Provide() => new(connectionString);
    }

    private sealed class CoordinatedStore(ConnectionProvider provider, Func<Task> barrier) : ApplicationUserOnlyStore(provider)
    {
        private bool _waited;

        protected override async Task<bool> TryUpdateTokenImplAsync(
                    SqliteConnection connection, ApplicationUserToken token, string? originalValue, CancellationToken cancellationToken)
        {
            if (!_waited)
            {
                _waited = true;
                var waitForConcurrentWrite = barrier ?? throw new InvalidOperationException("The test coordination callback is required.");
                await waitForConcurrentWrite();
            }

            return await base.TryUpdateTokenImplAsync(connection, token, originalValue, cancellationToken);
        }
    }
}
