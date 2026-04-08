using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity;
using Reqnroll;
using MySql.Data.MySqlClient;
using MySqlUserOnlyStoreBase =
    AdaskoTheBeAsT.Identity.Dapper.DapperUserOnlyStoreBase<
        AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity.ApplicationUser,
        System.Guid,
        AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity.ApplicationUserClaim,
        AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity.ApplicationUserLogin,
        AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity.ApplicationUserToken,
        MySql.Data.MySqlClient.MySqlConnection>;
using MySqlUserStoreBase =
    AdaskoTheBeAsT.Identity.Dapper.DapperUserStoreBase<
        AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity.ApplicationUser,
        AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity.ApplicationRole,
        System.Guid,
        AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity.ApplicationUserClaim,
        AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity.ApplicationUserRole,
        AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity.ApplicationUserLogin,
        AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Identity.ApplicationUserToken,
        MySql.Data.MySqlClient.MySqlConnection>;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.Steps;

[Binding]
public sealed class WithoutNormalizedAspNetIdentityGuidUserOnlyStoreStepDefinitions
    : GuidUserStoreTableDrivenStepDefinitionsBase<
        ApplicationUser,
        ApplicationRole,
        ApplicationUserClaim,
        ApplicationUserRole,
        ApplicationUserLogin,
        ApplicationUserToken,
        ApplicationRoleClaim,
        MySqlConnection>
{
    public WithoutNormalizedAspNetIdentityGuidUserOnlyStoreStepDefinitions(
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

    protected override MySqlUserOnlyStoreBase CreateUserStoreInstance() => CreateUserOnlyStore();

    protected override MySqlUserStoreBase CreateRoleCapableUserStoreInstance() => CreateUserStore();

    [Given("I have configured MySQL UserOnlyStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredUserOnlyStoreWithoutNormalizedAndGuidId() => ResetUserStoreScenarioAsync();

    [Given("I created users for MySQL UserOnlyStore")]
    public Task GivenICreatedUsersForUserOnlyStore(Table table) => CreateUsersFromTableAsync(table);

    [Given("I created roles for MySQL UserOnlyStore")]
    public Task GivenICreatedRolesForUserOnlyStore(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added user claims for MySQL UserOnlyStore")]
    public Task GivenIAddedUserClaimsForUserOnlyStore(Table table) => AddUserClaimsFromTableAsync(table);

    [Given("I added user logins for MySQL UserOnlyStore")]
    public Task GivenIAddedUserLoginsForUserOnlyStore(Table table) => AddUserLoginsFromTableAsync(table);

    [Given("I added user tokens for MySQL UserOnlyStore")]
    public Task GivenIAddedUserTokensForUserOnlyStore(Table table) => AddUserTokensFromTableAsync(table);

    [Given("I set authenticator keys for MySQL UserOnlyStore")]
    public Task GivenISetAuthenticatorKeysForUserOnlyStore(Table table) => SetAuthenticatorKeysFromTableAsync(table);

    [Given("I replaced recovery codes for MySQL UserOnlyStore")]
    public Task GivenIReplacedRecoveryCodesForUserOnlyStore(Table table) => ReplaceRecoveryCodesFromTableAsync(table);

    [Given("I added users to roles for MySQL UserOnlyStore")]
    public Task GivenIAddedUsersToRolesForUserOnlyStore(Table table) => AddUsersToRolesFromTableAsync(table);

    [When("I verify {string} on MySQL UserOnlyStore without normalized and Guid id")]
    public async Task WhenIVerifyMethodOnUserOnlyStoreWithoutNormalizedAndGuidId(string methodName)
    {
        await VerifyUserOnlyStoreMethodAsync(methodName);
        LastVerifiedMethodName = methodName;
    }

    [When("I execute {string} on MySQL UserOnlyStore")]
    public Task WhenIExecuteMethodOnUserOnlyStore(string methodName) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table: null);

    [When("I execute {string} on MySQL UserOnlyStore with parameters")]
    public Task WhenIExecuteMethodOnUserOnlyStoreWithParameters(string methodName, Table table) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table);

    [Then("the last identity result for MySQL UserOnlyStore should be successful")]
    public void ThenTheLastIdentityResultForUserOnlyStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last user result for MySQL UserOnlyStore should match")]
    public void ThenTheLastUserResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastUserMatches(table);

    [Then("the last user result for MySQL UserOnlyStore should be null")]
    public void ThenTheLastUserResultForUserOnlyStoreShouldBeNull() =>
        AssertLastUserIsNull();

    [Then("the last users result for MySQL UserOnlyStore should match")]
    public void ThenTheLastUsersResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastUsersMatch(table);

    [Then("the last claims result for MySQL UserOnlyStore should match")]
    public void ThenTheLastClaimsResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("the last logins result for MySQL UserOnlyStore should match")]
    public void ThenTheLastLoginsResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastLoginsMatch(table);

    [Then("the last string result for MySQL UserOnlyStore should be {string}")]
    public void ThenTheLastStringResultForUserOnlyStoreShouldBe(string expected) =>
        AssertLastString(expected);

    [Then("the last string result for MySQL UserOnlyStore should be null")]
    public void ThenTheLastStringResultForUserOnlyStoreShouldBeNull() =>
        AssertLastString(expected: null);

    [Then("the last boolean result for MySQL UserOnlyStore should be {string}")]
    public void ThenTheLastBooleanResultForUserOnlyStoreShouldBe(string expected) =>
        AssertLastBoolean(bool.Parse(expected));

    [Then("the last integer result for MySQL UserOnlyStore should be {int}")]
    public void ThenTheLastIntegerResultForUserOnlyStoreShouldBe(int expected) =>
        AssertLastInteger(expected);

    [Then("{string} on MySQL UserOnlyStore should work without normalized and Guid id")]
    public void ThenMethodOnUserOnlyStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }
}
