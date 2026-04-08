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
public sealed class WithoutNormalizedAspNetIdentityGuidUserOnlyStoreStepDefinitions
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

    protected override OracleUserOnlyStoreBase CreateUserStoreInstance() => CreateUserOnlyStore();

    protected override OracleUserStoreBase CreateRoleCapableUserStoreInstance() => CreateUserStore();

    [Given("I have configured Oracle UserOnlyStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredUserOnlyStoreWithoutNormalizedAndGuidId() => ResetUserStoreScenarioAsync();

    [Given("I created users for Oracle UserOnlyStore")]
    public Task GivenICreatedUsersForUserOnlyStore(Table table) => CreateUsersFromTableAsync(table);

    [Given("I created roles for Oracle UserOnlyStore")]
    public Task GivenICreatedRolesForUserOnlyStore(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added user claims for Oracle UserOnlyStore")]
    public Task GivenIAddedUserClaimsForUserOnlyStore(Table table) => AddUserClaimsFromTableAsync(table);

    [Given("I added user logins for Oracle UserOnlyStore")]
    public Task GivenIAddedUserLoginsForUserOnlyStore(Table table) => AddUserLoginsFromTableAsync(table);

    [Given("I added user tokens for Oracle UserOnlyStore")]
    public Task GivenIAddedUserTokensForUserOnlyStore(Table table) => AddUserTokensFromTableAsync(table);

    [Given("I set authenticator keys for Oracle UserOnlyStore")]
    public Task GivenISetAuthenticatorKeysForUserOnlyStore(Table table) => SetAuthenticatorKeysFromTableAsync(table);

    [Given("I replaced recovery codes for Oracle UserOnlyStore")]
    public Task GivenIReplacedRecoveryCodesForUserOnlyStore(Table table) => ReplaceRecoveryCodesFromTableAsync(table);

    [Given("I added users to roles for Oracle UserOnlyStore")]
    public Task GivenIAddedUsersToRolesForUserOnlyStore(Table table) => AddUsersToRolesFromTableAsync(table);

    [When("I verify {string} on Oracle UserOnlyStore without normalized and Guid id")]
    public async Task WhenIVerifyMethodOnUserOnlyStoreWithoutNormalizedAndGuidId(string methodName)
    {
        await VerifyUserOnlyStoreMethodAsync(methodName);
        LastVerifiedMethodName = methodName;
    }

    [When("I execute {string} on Oracle UserOnlyStore")]
    public Task WhenIExecuteMethodOnUserOnlyStore(string methodName) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table: null);

    [When("I execute {string} on Oracle UserOnlyStore with parameters")]
    public Task WhenIExecuteMethodOnUserOnlyStoreWithParameters(string methodName, Table table) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table);

    [Then("the last identity result for Oracle UserOnlyStore should be successful")]
    public void ThenTheLastIdentityResultForUserOnlyStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last user result for Oracle UserOnlyStore should match")]
    public void ThenTheLastUserResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastUserMatches(table);

    [Then("the last user result for Oracle UserOnlyStore should be null")]
    public void ThenTheLastUserResultForUserOnlyStoreShouldBeNull() =>
        AssertLastUserIsNull();

    [Then("the last users result for Oracle UserOnlyStore should match")]
    public void ThenTheLastUsersResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastUsersMatch(table);

    [Then("the last claims result for Oracle UserOnlyStore should match")]
    public void ThenTheLastClaimsResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("the last logins result for Oracle UserOnlyStore should match")]
    public void ThenTheLastLoginsResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastLoginsMatch(table);

    [Then("the last string result for Oracle UserOnlyStore should be {string}")]
    public void ThenTheLastStringResultForUserOnlyStoreShouldBe(string expected) =>
        AssertLastString(expected);

    [Then("the last string result for Oracle UserOnlyStore should be null")]
    public void ThenTheLastStringResultForUserOnlyStoreShouldBeNull() =>
        AssertLastString(expected: null);

    [Then("the last boolean result for Oracle UserOnlyStore should be {string}")]
    public void ThenTheLastBooleanResultForUserOnlyStoreShouldBe(string expected) =>
        AssertLastBoolean(bool.Parse(expected));

    [Then("the last integer result for Oracle UserOnlyStore should be {int}")]
    public void ThenTheLastIntegerResultForUserOnlyStoreShouldBe(int expected) =>
        AssertLastInteger(expected);

    [Then("{string} on Oracle UserOnlyStore should work without normalized and Guid id")]
    public void ThenMethodOnUserOnlyStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }
}
