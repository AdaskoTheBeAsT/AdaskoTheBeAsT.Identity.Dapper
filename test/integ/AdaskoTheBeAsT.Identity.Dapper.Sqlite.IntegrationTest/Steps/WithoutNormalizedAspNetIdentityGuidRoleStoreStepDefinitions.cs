using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity;
using Reqnroll;
using Microsoft.Data.Sqlite;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Steps;

[Binding]
public sealed class WithoutNormalizedAspNetIdentityGuidRoleStoreStepDefinitions
    : GuidRoleStoreTableDrivenStepDefinitionsBase<
        ApplicationUser,
        ApplicationRole,
        ApplicationUserClaim,
        ApplicationUserRole,
        ApplicationUserLogin,
        ApplicationUserToken,
        ApplicationRoleClaim,
        SqliteConnection>
{
    public WithoutNormalizedAspNetIdentityGuidRoleStoreStepDefinitions(
        FeatureContext featureContext,
        ScenarioContext scenarioContext)
        : base(
            featureContext,
            scenarioContext,
            () => TestStoreFactory.CreateRoleStore(),
            () => TestStoreFactory.CreateUserOnlyStore(),
            () => TestStoreFactory.CreateUserStore())
    {
    }

    [Given("I have configured SQLite RoleStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredRoleStoreWithoutNormalizedAndGuidId() => ResetRoleStoreScenarioAsync();

    [Given("I created roles for SQLite RoleStore")]
    public Task GivenICreatedRolesForRoleStore(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added role claims for SQLite RoleStore")]
    public Task GivenIAddedRoleClaimsForRoleStore(Table table) => AddRoleClaimsFromTableAsync(table);

    [When("I verify {string} on SQLite RoleStore without normalized and Guid id")]
    public Task WhenIVerifyMethodOnRoleStoreWithoutNormalizedAndGuidId(string methodName) =>
        VerifyRoleStoreMethodAndRememberAsync(methodName);

    [When("I execute {string} on SQLite RoleStore")]
    public Task WhenIExecuteMethodOnRoleStore(string methodName) => ExecuteRoleStoreMethodAsync(methodName);

    [When("I execute {string} on SQLite RoleStore with parameters")]
    public Task WhenIExecuteMethodOnRoleStoreWithParameters(string methodName, Table table) =>
        ExecuteRoleStoreMethodAsync(methodName, table);

    [Then("the last identity result for SQLite RoleStore should be successful")]
    public void ThenTheLastIdentityResultForRoleStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last role result for SQLite RoleStore should match")]
    public void ThenTheLastRoleResultForRoleStoreShouldMatch(Table table) =>
        AssertLastRoleMatches(table);

    [Then("the last role result for SQLite RoleStore should be null")]
    public void ThenTheLastRoleResultForRoleStoreShouldBeNull() =>
        AssertLastRoleIsNull();

    [Then("the last roles result for SQLite RoleStore should match")]
    public void ThenTheLastRolesResultForRoleStoreShouldMatch(Table table) =>
        AssertLastRolesMatch(table);

    [Then("the last claims result for SQLite RoleStore should match")]
    public void ThenTheLastClaimsResultForRoleStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("{string} on SQLite RoleStore should work without normalized and Guid id")]
    public void ThenMethodOnRoleStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }
}
