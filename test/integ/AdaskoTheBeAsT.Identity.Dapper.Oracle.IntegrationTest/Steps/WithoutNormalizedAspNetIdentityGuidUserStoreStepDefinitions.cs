using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest;
using AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity;
using Reqnroll;
using Oracle.ManagedDataAccess.Client;
using OracleUserOnlyStoreBase =
    AdaskoTheBeAsT.Identity.Dapper.DapperUserOnlyStoreBase<
        AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity.ApplicationUser,
        System.Guid,
        AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity.ApplicationUserClaim,
        AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity.ApplicationUserLogin,
        AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity.ApplicationUserToken,
        Oracle.ManagedDataAccess.Client.OracleConnection>;
using OracleUserStoreBase =
    AdaskoTheBeAsT.Identity.Dapper.DapperUserStoreBase<
        AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity.ApplicationUser,
        AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity.ApplicationRole,
        System.Guid,
        AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity.ApplicationUserClaim,
        AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity.ApplicationUserRole,
        AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity.ApplicationUserLogin,
        AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity.ApplicationUserToken,
        Oracle.ManagedDataAccess.Client.OracleConnection>;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Steps;

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
        OracleConnection>
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

    protected override OracleUserOnlyStoreBase CreateUserStoreInstance() => CreateUserStore();

    protected override OracleUserStoreBase CreateRoleCapableUserStoreInstance() => CreateUserStore();

    [Given("I have configured Oracle UserStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredUserStoreWithoutNormalizedAndGuidId() => ResetUserStoreScenarioAsync();

    [Given("I created users for Oracle UserStore")]
    public Task GivenICreatedUsersForUserStore(Table table) => CreateUsersFromTableAsync(table);

    [Given("I created roles for Oracle UserStore")]
    public Task GivenICreatedRolesForUserStore(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added user claims for Oracle UserStore")]
    public Task GivenIAddedUserClaimsForUserStore(Table table) => AddUserClaimsFromTableAsync(table);

    [Given("I added role claims for Oracle UserStore")]
    public Task GivenIAddedRoleClaimsForUserStore(Table table) => AddRoleClaimsFromTableAsync(table);

    [Given("I added user logins for Oracle UserStore")]
    public Task GivenIAddedUserLoginsForUserStore(Table table) => AddUserLoginsFromTableAsync(table);

    [Given("I added user tokens for Oracle UserStore")]
    public Task GivenIAddedUserTokensForUserStore(Table table) => AddUserTokensFromTableAsync(table);

    [Given("I set authenticator keys for Oracle UserStore")]
    public Task GivenISetAuthenticatorKeysForUserStore(Table table) => SetAuthenticatorKeysFromTableAsync(table);

    [Given("I replaced recovery codes for Oracle UserStore")]
    public Task GivenIReplacedRecoveryCodesForUserStore(Table table) => ReplaceRecoveryCodesFromTableAsync(table);

    [Given("I added users to roles for Oracle UserStore")]
    public Task GivenIAddedUsersToRolesForUserStore(Table table) => AddUsersToRolesFromTableAsync(table);

    [When("I verify {string} on Oracle UserStore without normalized and Guid id")]
    public async Task WhenIVerifyMethodOnUserStoreWithoutNormalizedAndGuidId(string methodName)
    {
        await VerifyUserStoreMethodAsync(methodName);
        LastVerifiedMethodName = methodName;
    }

    [When("I execute {string} on Oracle UserStore")]
    public Task WhenIExecuteMethodOnUserStore(string methodName) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table: null);

    [When("I execute {string} on Oracle UserStore with parameters")]
    public Task WhenIExecuteMethodOnUserStoreWithParameters(string methodName, Table table) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table);

    [Then("the last identity result for Oracle UserStore should be successful")]
    public void ThenTheLastIdentityResultForUserStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last user result for Oracle UserStore should match")]
    public void ThenTheLastUserResultForUserStoreShouldMatch(Table table) =>
        AssertLastUserMatches(table);

    [Then("the last user result for Oracle UserStore should be null")]
    public void ThenTheLastUserResultForUserStoreShouldBeNull() =>
        AssertLastUserIsNull();

    [Then("the last users result for Oracle UserStore should match")]
    public void ThenTheLastUsersResultForUserStoreShouldMatch(Table table) =>
        AssertLastUsersMatch(table);

    [Then("the last claims result for Oracle UserStore should match")]
    public void ThenTheLastClaimsResultForUserStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("the last logins result for Oracle UserStore should match")]
    public void ThenTheLastLoginsResultForUserStoreShouldMatch(Table table) =>
        AssertLastLoginsMatch(table);

    [Then("the last strings result for Oracle UserStore should match")]
    public void ThenTheLastStringsResultForUserStoreShouldMatch(Table table) =>
        AssertLastStringsMatch(table);

    [Then("the last string result for Oracle UserStore should be {string}")]
    public void ThenTheLastStringResultForUserStoreShouldBe(string expected) =>
        AssertLastString(expected);

    [Then("the last string result for Oracle UserStore should be null")]
    public void ThenTheLastStringResultForUserStoreShouldBeNull() =>
        AssertLastString(expected: null);

    [Then("the last boolean result for Oracle UserStore should be {string}")]
    public void ThenTheLastBooleanResultForUserStoreShouldBe(string expected) =>
        AssertLastBoolean(bool.Parse(expected));

    [Then("the last integer result for Oracle UserStore should be {int}")]
    public void ThenTheLastIntegerResultForUserStoreShouldBe(int expected) =>
        AssertLastInteger(expected);

    [Then("{string} on Oracle UserStore should work without normalized and Guid id")]
    public void ThenMethodOnUserStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }
}
