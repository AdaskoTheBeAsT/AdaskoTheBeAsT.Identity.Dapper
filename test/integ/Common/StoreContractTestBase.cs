using System.Data;
using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;

public abstract class StoreContractTestBase<TUser, TRole, TUserClaim, TUserLogin, TUserToken, TRoleClaim, TConnection>
    where TUser : IdentityUser<Guid>, new()
    where TRole : IdentityRole<Guid>, new()
    where TUserClaim : IdentityUserClaim<Guid>, new()
    where TUserLogin : IdentityUserLogin<Guid>, new()
    where TUserToken : IdentityUserToken<Guid>, new()
    where TRoleClaim : IdentityRoleClaim<Guid>, new()
    where TConnection : IDbConnection
{
    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 1001)]
    public async Task PagingRejectsInvalidBoundsBeforeOpeningConnections(int offset, int pageSize)
    {
        var provider = new Mock<IIdentityDbConnectionProvider<TConnection>>(MockBehavior.Strict);
        using var users = new LegacyUsers(provider.Object);
        using var roles = new LegacyRoles(provider.Object);
        await FluentActions.Awaiting(() => users.GetUsersPageAsync(offset, pageSize, TestContext.Current.CancellationToken)).Should().ThrowExactlyAsync<ArgumentOutOfRangeException>();
        await FluentActions.Awaiting(() => roles.GetRolesPageAsync(offset, pageSize, TestContext.Current.CancellationToken)).Should().ThrowExactlyAsync<ArgumentOutOfRangeException>();
        provider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PagingRejectsLegacySqlAndCancellationBeforeOpeningConnections()
    {
        var provider = new Mock<IIdentityDbConnectionProvider<TConnection>>(MockBehavior.Strict);
        using var users = new LegacyUsers(provider.Object);
        using var roles = new LegacyRoles(provider.Object);
        await FluentActions.Awaiting(() => users.GetUsersPageAsync(0, 1, TestContext.Current.CancellationToken)).Should().ThrowExactlyAsync<NotSupportedException>();
        await FluentActions.Awaiting(() => roles.GetRolesPageAsync(0, 1, TestContext.Current.CancellationToken)).Should().ThrowExactlyAsync<NotSupportedException>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await FluentActions.Awaiting(() => users.GetUsersPageAsync(0, 1, cancellation.Token)).Should().ThrowAsync<OperationCanceledException>();
        await FluentActions.Awaiting(() => roles.GetRolesPageAsync(0, 1, cancellation.Token)).Should().ThrowAsync<OperationCanceledException>();
        provider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ClaimBatchesPreserveDuplicatesAndCrossChunkBoundaries()
    {
        using var store = Users();
        var user = NewUser();
        Success(await store.CreateAsync(user, CancellationToken.None));
        var claims = Enumerable.Range(0, 70).Select(i => new Claim("batch", "value-" + i)).ToList();
        claims.Add(new Claim("batch", "value-0"));
        await store.AddClaimsAsync(user, claims, CancellationToken.None);
        (await store.GetClaimsAsync(user, CancellationToken.None)).Should().HaveCount(71);
        await store.RemoveClaimsAsync(user, claims.Take(66), CancellationToken.None);
        var remaining = await store.GetClaimsAsync(user, CancellationToken.None);
        remaining.Should().HaveCount(4);
        remaining.Select(claim => claim.Value).OrderBy(value => value, StringComparer.Ordinal).Should().Equal(StoreContractTestData.RemainingClaims);
        await store.RemoveClaimsAsync(user, remaining, CancellationToken.None);
        (await store.GetClaimsAsync(user, CancellationToken.None)).Should().BeEmpty();
        await store.AddClaimsAsync(user, Array.Empty<Claim>(), CancellationToken.None);
        await store.RemoveClaimsAsync(user, Array.Empty<Claim>(), CancellationToken.None);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("initial")]
    public async Task UserWritesRejectStaleAndDeletedRows(string? stamp)
    {
        using var store = Users();
        var user = NewUser();
        user.ConcurrencyStamp = stamp;
        Success(await store.CreateAsync(user, CancellationToken.None));
        var stale = (await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None))!;
        stale.ConcurrencyStamp.Should().Be(stamp);
        store.Users.Single(u => u.Id == user.Id).ConcurrencyStamp.Should().Be(stamp);
        (await store.FindByNameAsync(user.UserName!, CancellationToken.None))!.ConcurrencyStamp.Should().Be(stamp);
        user.PhoneNumber = "current";
        Success(await store.UpdateAsync(user, CancellationToken.None));
        user.ConcurrencyStamp.Should().NotBe(stamp);
        ConcurrencyFailure(await store.UpdateAsync(stale, CancellationToken.None));
        ConcurrencyFailure(await store.DeleteAsync(stale, CancellationToken.None));
        stale.ConcurrencyStamp.Should().Be(stamp);
        (await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None))!.PhoneNumber.Should().Be("current");
        Success(await store.DeleteAsync(user, CancellationToken.None));
        ConcurrencyFailure(await store.UpdateAsync(user, CancellationToken.None));
        ConcurrencyFailure(await store.DeleteAsync(user, CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("initial")]
    public async Task RoleWritesRejectStaleAndDeletedRows(string? stamp)
    {
        using var store = Roles();
        var role = new TRole { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString(), ConcurrencyStamp = stamp };
        Success(await store.CreateAsync(role, CancellationToken.None));
        var stale = (await store.FindByIdAsync(role.Id.ToString(), CancellationToken.None))!;
        stale.ConcurrencyStamp.Should().Be(stamp);
        store.Roles.Single(r => r.Id == role.Id).ConcurrencyStamp.Should().Be(stamp);
        (await store.FindByNameAsync(role.Name, CancellationToken.None))!.ConcurrencyStamp.Should().Be(stamp);
        Success(await store.UpdateAsync(role, CancellationToken.None));
        role.ConcurrencyStamp.Should().NotBe(stamp);
        ConcurrencyFailure(await store.UpdateAsync(stale, CancellationToken.None));
        ConcurrencyFailure(await store.DeleteAsync(stale, CancellationToken.None));
        stale.ConcurrencyStamp.Should().Be(stamp);
        Success(await store.DeleteAsync(role, CancellationToken.None));
        ConcurrencyFailure(await store.UpdateAsync(role, CancellationToken.None));
        ConcurrencyFailure(await store.DeleteAsync(role, CancellationToken.None));
    }

    [Fact]
    public async Task RecoveryCodesRejectEmptyInputAndCaseVariantReplay()
    {
        using var store = Users();
        var user = NewUser();
        Success(await store.CreateAsync(user, CancellationToken.None));
        (await store.RedeemCodeAsync(user, string.Empty, CancellationToken.None)).Should().BeFalse();
        await store.ReplaceCodesAsync(user, StoreContractTestData.CaseVariantRecoveryCodes, CancellationToken.None);
        (await store.RedeemCodeAsync(user, "OnE", CancellationToken.None)).Should().BeTrue();
        (await store.RedeemCodeAsync(user, "one", CancellationToken.None)).Should().BeFalse();
        (await store.CountCodesAsync(user, CancellationToken.None)).Should().Be(1);
        (await store.RedeemCodeAsync(user, "two", CancellationToken.None)).Should().BeTrue();
        (await store.RedeemCodeAsync(user, string.Empty, CancellationToken.None)).Should().BeFalse();
        (await store.CountCodesAsync(user, CancellationToken.None)).Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentRedemptionsDoNotReplayOrRestoreCodesAsync(bool differentCodes)
    {
        using var store = Users();
        var user = NewUser();
        Success(await store.CreateAsync(user, CancellationToken.None));
        await store.ReplaceCodesAsync(user, StoreContractTestData.ConcurrentRecoveryCodes, CancellationToken.None);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrived = 0;
        Task BarrierAsync()
        {
            if (Interlocked.Increment(ref arrived) == 2)
            {
                ready.SetResult();
            }

#pragma warning disable IDISP013 // Both barrier callbacks complete in the awaited WhenAll before the store leaves scope.
            return ready.Task.WaitAsync(TimeSpan.FromSeconds(20));
#pragma warning restore IDISP013
        }

        var results = await Task.WhenAll(
            RedeemAsync(user, "first", BarrierAsync),
            RedeemAsync(user, differentCodes ? "second" : "first", BarrierAsync));
        results.Count(x => x).Should().Be(differentCodes ? 2 : 1);
        (await store.RedeemCodeAsync(user, "first", CancellationToken.None)).Should().BeFalse();
        (await store.CountCodesAsync(user, CancellationToken.None)).Should().Be(differentCodes ? 0 : 1);
    }

    [Theory]
    [InlineData("alpha", "ALPHA")]
    [InlineData("alpha ", "alpha")]
    [InlineData("café", "cafe")]
    [InlineData(null, "alpha")]
    [InlineData("alpha", null)]
    public async Task TokenCompareExchangeUsesExactNullSafeValue(string? current, string? stale)
    {
        using var store = Users();
        var user = NewUser();
        Success(await store.CreateAsync(user, CancellationToken.None));
        await store.SetTokenAsync(user, "contract", "exact", current, CancellationToken.None);
        var token = new TUserToken { UserId = user.Id, LoginProvider = "contract", Name = "exact", Value = "replacement" };
        (await CompareExchangeAsync(token, stale)).Should().BeFalse();
        (await store.GetTokenAsync(user, "contract", "exact", CancellationToken.None)).Should().Be(current);
        (await CompareExchangeAsync(token, current)).Should().BeTrue();
        (await store.GetTokenAsync(user, "contract", "exact", CancellationToken.None)).Should().Be("replacement");
    }

    [Fact]
    public async Task NullAndLongTokensCanBeUpdatedAndRemoved()
    {
        using var store = Users();
        var user = NewUser();
        Success(await store.CreateAsync(user, CancellationToken.None));
        await store.SetTokenAsync(user, "contract", "long", null, CancellationToken.None);
        var value = new string('a', 256);
        await store.SetTokenAsync(user, "contract", "long", value, CancellationToken.None);
        (await store.GetTokenAsync(user, "contract", "long", CancellationToken.None)).Should().Be(value);
        await store.RemoveTokenAsync(user, "contract", "long", CancellationToken.None);
        (await store.GetTokenAsync(user, "contract", "long", CancellationToken.None)).Should().BeNull();
    }

    [Theory]
    [InlineData("find")]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("update")]
    public async Task TokenCommandsPropagateCancellation(string operation)
    {
        // Exercise the protected commands directly, not just public entry-point checks.
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await FluentActions.Awaiting(() => TokenCommandAsync(NewUser(), operation, cancellation.Token)).Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CreateSurfacesInFlightCancellationInsteadOfFailedResult()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new Mock<IIdentityDbConnectionProvider<TConnection>>(MockBehavior.Strict);

        // The overridden operation never uses the connection.
        provider.Setup(p => p.Provide()).Returns(default(TConnection)!);
        using var store = new CancelingUsers(provider.Object);
        var create = store.CreateAsync(NewUser(), cancellation.Token);
#pragma warning disable VSTHRD003 // Observe the test-owned gate signaled by the already-started create operation.
        await store.Entered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003
        await cancellation.CancelAsync();
#pragma warning disable VSTHRD003 // Observe cancellation of the create operation started by this test.
        await FluentActions.Awaiting(() => create).Should().ThrowAsync<OperationCanceledException>();
#pragma warning restore VSTHRD003
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacySqlIsRejectedBeforeOpeningConnection(bool delete)
    {
        var provider = new Mock<IIdentityDbConnectionProvider<TConnection>>(MockBehavior.Strict);
        using var userStore = new LegacyUsers(provider.Object);
        using var roleStore = new LegacyRoles(provider.Object);
        var userError = (await FluentActions.Awaiting(() =>
            delete ? userStore.DeleteAsync(NewUser(), CancellationToken.None) : userStore.UpdateAsync(NewUser(), CancellationToken.None)).Should().ThrowExactlyAsync<NotSupportedException>()).Which;
        var roleError = (await FluentActions.Awaiting(() =>
            delete ? roleStore.DeleteAsync(new TRole(), CancellationToken.None) : roleStore.UpdateAsync(new TRole(), CancellationToken.None)).Should().ThrowExactlyAsync<NotSupportedException>()).Which;
        userError.Message.Should().Contain("Regenerate");
        roleError.Message.Should().Contain("Regenerate");
        provider.VerifyNoOtherCalls();
    }

    protected abstract DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TConnection> Users();

    protected abstract DapperRoleStoreBase<TRole, Guid, TRoleClaim, TConnection> Roles();

    protected abstract Task<bool> CompareExchangeAsync(TUserToken token, string? original);

    protected abstract Task<bool> RedeemAsync(TUser user, string code, Func<Task> barrier);

    protected abstract Task TokenCommandAsync(TUser user, string operation, CancellationToken cancellationToken);

    private static TUser NewUser() => new()
    {
        Id = Guid.NewGuid(),
        UserName = Guid.NewGuid().ToString(),
        SecurityStamp = Guid.NewGuid().ToString(),
        ConcurrencyStamp = Guid.NewGuid().ToString(),
    };

    private static void Success(IdentityResult result) =>
                result.Succeeded.Should().BeTrue("{0}", string.Join("; ", result.Errors.Select(e => e.Description)));

    private static void ConcurrencyFailure(IdentityResult result)
    {
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == nameof(ConcurrencyFailure));
    }

    private sealed class LegacyUsers(IIdentityDbConnectionProvider<TConnection> provider)
                : DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TConnection>(
                    new IdentityErrorDescriber(), provider, Mock.Of<IIdentityUserSql>(MockBehavior.Strict), Mock.Of<IIdentityUserClaimSql>(MockBehavior.Strict),
                    Mock.Of<IIdentityUserLoginSql>(MockBehavior.Strict), Mock.Of<IIdentityUserTokenSql>(MockBehavior.Strict));

    private sealed class CancelingUsers(IIdentityDbConnectionProvider<TConnection> provider)
                : DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TConnection>(
                    new IdentityErrorDescriber(), provider, Mock.Of<IIdentityUserSql>(MockBehavior.Strict), Mock.Of<IIdentityUserClaimSql>(MockBehavior.Strict),
                    Mock.Of<IIdentityUserLoginSql>(MockBehavior.Strict), Mock.Of<IIdentityUserTokenSql>(MockBehavior.Strict))
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => _entered.Task;

        protected override Task CreateImplAsync(TConnection connection, TUser user, CancellationToken cancellationToken)
        {
            _entered.SetResult();
            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class LegacyRoles(IIdentityDbConnectionProvider<TConnection> provider)
                : DapperRoleStoreBase<TRole, Guid, TRoleClaim, TConnection>(
                    new IdentityErrorDescriber(), provider, Mock.Of<IIdentityRoleSql>(MockBehavior.Strict), Mock.Of<IIdentityRoleClaimSql>(MockBehavior.Strict));
}
