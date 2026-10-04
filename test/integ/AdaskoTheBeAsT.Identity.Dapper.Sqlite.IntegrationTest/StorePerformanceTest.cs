using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.TestCollections;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest;

public sealed class StorePerformanceTest(DatabaseWithGuidIdFixture fixture) : IClassFixture<DatabaseWithGuidIdFixture>
{
    [Fact]
    public async Task ClaimsUseBoundedBatchesRatherThanOneCommandPerClaim()
    {
        using var store = new CountingStore(new Provider(fixture.ConnectionString));
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString() };
        (await store.CreateAsync(user, TestContext.Current.CancellationToken)).Succeeded.Should().BeTrue();
        var claims = Enumerable.Range(0, 71).Select(index => new Claim("batch", index.ToString(System.Globalization.CultureInfo.InvariantCulture))).ToArray();

        await store.AddClaimsAsync(user, claims, TestContext.Current.CancellationToken);
        store.BatchSizes.Should().Equal(new[] { 32, 32, 7 });
        (await store.GetClaimsAsync(user, TestContext.Current.CancellationToken)).Count.Should().Be(71);
        store.BatchSizes.Clear();
        await store.RemoveClaimsAsync(user, claims, TestContext.Current.CancellationToken);
        store.BatchSizes.Should().Equal(new[] { 32, 32, 7 });
        (await store.GetClaimsAsync(user, TestContext.Current.CancellationToken)).Should().BeEmpty();
        store.BatchSizes.Clear();
        await store.AddClaimsAsync(user, Array.Empty<Claim>(), TestContext.Current.CancellationToken);
        await store.RemoveClaimsAsync(user, Array.Empty<Claim>(), TestContext.Current.CancellationToken);
        store.BatchSizes.Should().BeEmpty();
    }

    private sealed class Provider(string connectionString) : IIdentityDbConnectionProvider<SqliteConnection>
    {
        public SqliteConnection Provide() => new(connectionString);
    }

    private sealed class CountingStore(Provider provider) : ApplicationUserOnlyStore(provider)
    {
        public List<int> BatchSizes { get; } = new();

        protected override object CreateClaimBatchParameters(
            IReadOnlyList<ApplicationUserClaim> claims, IReadOnlyList<string> parameterNames)
        {
            BatchSizes.Add(claims.Count);
            return base.CreateClaimBatchParameters(claims, parameterNames);
        }
    }
}
