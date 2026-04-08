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
public sealed class WithoutNormalizedAspNetIdentityGuidUserStoreStepDefinitions
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
    public WithoutNormalizedAspNetIdentityGuidUserStoreStepDefinitions(
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

    protected override MySqlUserOnlyStoreBase CreateUserStoreInstance() => CreateUserStore();

    protected override MySqlUserStoreBase CreateRoleCapableUserStoreInstance() => CreateUserStore();

    [Given("I have configured MySQL UserStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredUserStoreWithoutNormalizedAndGuidId() => ResetUserStoreScenarioAsync();

    [Given("I created users for MySQL UserStore")]
    public Task GivenICreatedUsersForUserStore(Table table) => CreateUsersFromTableAsync(table);

    [Given("I created roles for MySQL UserStore")]
    public Task GivenICreatedRolesForUserStore(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added user claims for MySQL UserStore")]
    public Task GivenIAddedUserClaimsForUserStore(Table table) => AddUserClaimsFromTableAsync(table);

    [Given("I added role claims for MySQL UserStore")]
    public Task GivenIAddedRoleClaimsForUserStore(Table table) => AddRoleClaimsFromTableAsync(table);

    [Given("I added user logins for MySQL UserStore")]
    public Task GivenIAddedUserLoginsForUserStore(Table table) => AddUserLoginsFromTableAsync(table);

    [Given("I added user tokens for MySQL UserStore")]
    public Task GivenIAddedUserTokensForUserStore(Table table) => AddUserTokensFromTableAsync(table);

    [Given("I set authenticator keys for MySQL UserStore")]
    public Task GivenISetAuthenticatorKeysForUserStore(Table table) => SetAuthenticatorKeysFromTableAsync(table);

    [Given("I replaced recovery codes for MySQL UserStore")]
    public Task GivenIReplacedRecoveryCodesForUserStore(Table table) => ReplaceRecoveryCodesFromTableAsync(table);

    [Given("I added users to roles for MySQL UserStore")]
    public Task GivenIAddedUsersToRolesForUserStore(Table table) => AddUsersToRolesFromTableAsync(table);

    [When("I verify {string} on MySQL UserStore without normalized and Guid id")]
    public async Task WhenIVerifyMethodOnUserStoreWithoutNormalizedAndGuidId(string methodName)
    {
        await VerifyUserStoreMethodAsync(methodName);
        LastVerifiedMethodName = methodName;
    }

    [When("I execute {string} on MySQL UserStore")]
    public Task WhenIExecuteMethodOnUserStore(string methodName) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table: null);

    [When("I execute {string} on MySQL UserStore with parameters")]
    public Task WhenIExecuteMethodOnUserStoreWithParameters(string methodName, Table table) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table);

    [Then("the last identity result for MySQL UserStore should be successful")]
    public void ThenTheLastIdentityResultForUserStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last user result for MySQL UserStore should match")]
    public void ThenTheLastUserResultForUserStoreShouldMatch(Table table) =>
        AssertLastUserMatches(table);

    [Then("the last user result for MySQL UserStore should be null")]
    public void ThenTheLastUserResultForUserStoreShouldBeNull() =>
        AssertLastUserIsNull();

    [Then("the last users result for MySQL UserStore should match")]
    public void ThenTheLastUsersResultForUserStoreShouldMatch(Table table) =>
        AssertLastUsersMatch(table);

    [Then("the last claims result for MySQL UserStore should match")]
    public void ThenTheLastClaimsResultForUserStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("the last logins result for MySQL UserStore should match")]
    public void ThenTheLastLoginsResultForUserStoreShouldMatch(Table table) =>
        AssertLastLoginsMatch(table);

    [Then("the last strings result for MySQL UserStore should match")]
    public void ThenTheLastStringsResultForUserStoreShouldMatch(Table table) =>
        AssertLastStringsMatch(table);

    [Then("the last string result for MySQL UserStore should be {string}")]
    public void ThenTheLastStringResultForUserStoreShouldBe(string expected) =>
        AssertLastString(expected);

    [Then("the last string result for MySQL UserStore should be null")]
    public void ThenTheLastStringResultForUserStoreShouldBeNull() =>
        AssertLastString(expected: null);

    [Then("the last boolean result for MySQL UserStore should be {string}")]
    public void ThenTheLastBooleanResultForUserStoreShouldBe(string expected) =>
        AssertLastBoolean(bool.Parse(expected));

    [Then("the last integer result for MySQL UserStore should be {int}")]
    public void ThenTheLastIntegerResultForUserStoreShouldBe(int expected) =>
        AssertLastInteger(expected);

    [Then("{string} on MySQL UserStore should work without normalized and Guid id")]
    public void ThenMethodOnUserStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }
}
