using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.TestCollections;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Steps;

[Binding]
public sealed class WithoutNormalizedAspNetIdentityGuidUserStoreStepDefinitions(
    DatabaseWithGuidIdFixture databaseWithGuidIdFixture)
    : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private IUserStore<ApplicationUser> _sut = null!;
    private IdentityResult? _result;

    [Given("I have configured Identity Connection Provider without normalized and Guid id")]
    public void GivenIHaveConfiguredIdentityConnectionProviderWithoutNormalizedAndGuidId()
    {
        var mockConnectionProvider = new Mock<IIdentityDbConnectionProvider<SqliteConnection>>(MockBehavior.Strict);

#pragma warning disable IDISP004,CA2000 // Don't ignore created IDisposable
        mockConnectionProvider.Setup(x => x.Provide())
            .Returns(databaseWithGuidIdFixture.Connection);
#pragma warning restore IDISP004,CA2000 // Don't ignore created IDisposable

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockConnectionProvider.Object);
        serviceCollection.AddScoped<IUserStore<ApplicationUser>, ApplicationUserStore>();
        _serviceProvider?.Dispose();
        _serviceProvider = serviceCollection.BuildServiceProvider();
        _sut = _serviceProvider.GetRequiredService<IUserStore<ApplicationUser>>();
    }

    [When("I add the user without normalized and Guid id to the user store")]
    public async Task WhenIAddTheUserWithoutNormalizedAndGuidIdToTheUserStoreAsync()
    {
        var user = new Faker<ApplicationUser>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.UserName, f => f.Person.UserName)
            .RuleFor(u => u.Email, f => f.Person.Email)
            .RuleFor(u => u.PasswordHash, f => f.Internet.Password())
            .RuleFor(u => u.PhoneNumberConfirmed, _ => true)
            .Generate();

        _result = await _sut.CreateAsync(user, CancellationToken.None);
    }

    [Then("the user without normalized and Guid id should be in the user store")]
    public void ThenTheUserWithoutNormalizedAndGuidIdShouldBeInTheUserStore()
    {
        _result.Should().Be(IdentityResult.Success);
    }

    public void Dispose()
    {
        _sut.Dispose();
        _serviceProvider?.Dispose();
    }
}
