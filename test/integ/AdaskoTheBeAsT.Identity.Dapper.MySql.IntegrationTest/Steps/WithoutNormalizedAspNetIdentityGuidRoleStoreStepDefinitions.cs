using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity;
using Reqnroll;
using MySql.Data.MySqlClient;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Steps;

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
        MySqlConnection>
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

    [Given("I have configured MySQL RoleStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredRoleStoreWithoutNormalizedAndGuidId() => ResetRoleStoreScenarioAsync();

    [Given("I created roles for MySQL RoleStore")]
    public Task GivenICreatedRolesForRoleStore(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added role claims for MySQL RoleStore")]
    public Task GivenIAddedRoleClaimsForRoleStore(Table table) => AddRoleClaimsFromTableAsync(table);

    [When("I verify {string} on MySQL RoleStore without normalized and Guid id")]
    public Task WhenIVerifyMethodOnRoleStoreWithoutNormalizedAndGuidId(string methodName) =>
        VerifyRoleStoreMethodAndRememberAsync(methodName);

    [When("I execute {string} on MySQL RoleStore")]
    public Task WhenIExecuteMethodOnRoleStore(string methodName) => ExecuteRoleStoreMethodAsync(methodName);

    [When("I execute {string} on MySQL RoleStore with parameters")]
    public Task WhenIExecuteMethodOnRoleStoreWithParameters(string methodName, Table table) =>
        ExecuteRoleStoreMethodAsync(methodName, table);

    [Then("the last identity result for MySQL RoleStore should be successful")]
    public void ThenTheLastIdentityResultForRoleStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last role result for MySQL RoleStore should match")]
    public void ThenTheLastRoleResultForRoleStoreShouldMatch(Table table) =>
        AssertLastRoleMatches(table);

    [Then("the last role result for MySQL RoleStore should be null")]
    public void ThenTheLastRoleResultForRoleStoreShouldBeNull() =>
        AssertLastRoleIsNull();

    [Then("the last roles result for MySQL RoleStore should match")]
    public void ThenTheLastRolesResultForRoleStoreShouldMatch(Table table) =>
        AssertLastRolesMatch(table);

    [Then("the last claims result for MySQL RoleStore should match")]
    public void ThenTheLastClaimsResultForRoleStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("{string} on MySQL RoleStore should work without normalized and Guid id")]
    public void ThenMethodOnRoleStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }
}
