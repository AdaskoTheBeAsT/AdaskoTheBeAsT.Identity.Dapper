using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity;
using Reqnroll;
using Microsoft.Data.Sqlite;
using SqliteUserOnlyStoreBase =
    AdaskoTheBeAsT.Identity.Dapper.DapperUserOnlyStoreBase<
        AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity.ApplicationUser,
        System.Guid,
        AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity.ApplicationUserClaim,
        AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity.ApplicationUserLogin,
        AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity.ApplicationUserToken,
        Microsoft.Data.Sqlite.SqliteConnection>;
using SqliteUserStoreBase =
    AdaskoTheBeAsT.Identity.Dapper.DapperUserStoreBase<
        AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity.ApplicationUser,
        AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity.ApplicationRole,
        System.Guid,
        AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity.ApplicationUserClaim,
        AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity.ApplicationUserRole,
        AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity.ApplicationUserLogin,
        AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity.ApplicationUserToken,
        Microsoft.Data.Sqlite.SqliteConnection>;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Steps;

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
        SqliteConnection>
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

    protected override SqliteUserOnlyStoreBase CreateUserStoreInstance() => CreateUserOnlyStore();

    protected override SqliteUserStoreBase CreateRoleCapableUserStoreInstance() => CreateUserStore();

    [Given("I have configured SQLite UserOnlyStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredUserOnlyStoreWithoutNormalizedAndGuidId() => ResetUserStoreScenarioAsync();

    [Given("I created users for SQLite UserOnlyStore")]
    public Task GivenICreatedUsersForUserOnlyStore(Table table) => CreateUsersFromTableAsync(table);

    [Given("I created roles for SQLite UserOnlyStore")]
    public Task GivenICreatedRolesForUserOnlyStore(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added user claims for SQLite UserOnlyStore")]
    public Task GivenIAddedUserClaimsForUserOnlyStore(Table table) => AddUserClaimsFromTableAsync(table);

    [Given("I added user logins for SQLite UserOnlyStore")]
    public Task GivenIAddedUserLoginsForUserOnlyStore(Table table) => AddUserLoginsFromTableAsync(table);

    [Given("I added user tokens for SQLite UserOnlyStore")]
    public Task GivenIAddedUserTokensForUserOnlyStore(Table table) => AddUserTokensFromTableAsync(table);

    [Given("I set authenticator keys for SQLite UserOnlyStore")]
    public Task GivenISetAuthenticatorKeysForUserOnlyStore(Table table) => SetAuthenticatorKeysFromTableAsync(table);

    [Given("I replaced recovery codes for SQLite UserOnlyStore")]
    public Task GivenIReplacedRecoveryCodesForUserOnlyStore(Table table) => ReplaceRecoveryCodesFromTableAsync(table);

    [Given("I added users to roles for SQLite UserOnlyStore")]
    public Task GivenIAddedUsersToRolesForUserOnlyStore(Table table) => AddUsersToRolesFromTableAsync(table);

    [When("I verify {string} on SQLite UserOnlyStore without normalized and Guid id")]
    public async Task WhenIVerifyMethodOnUserOnlyStoreWithoutNormalizedAndGuidId(string methodName)
    {
        await VerifyUserOnlyStoreMethodAsync(methodName);
        LastVerifiedMethodName = methodName;
    }

    [When("I execute {string} on SQLite UserOnlyStore")]
    public Task WhenIExecuteMethodOnUserOnlyStore(string methodName) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table: null);

    [When("I execute {string} on SQLite UserOnlyStore with parameters")]
    public Task WhenIExecuteMethodOnUserOnlyStoreWithParameters(string methodName, Table table) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table);

    [Then("the last identity result for SQLite UserOnlyStore should be successful")]
    public void ThenTheLastIdentityResultForUserOnlyStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last user result for SQLite UserOnlyStore should match")]
    public void ThenTheLastUserResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastUserMatches(table);

    [Then("the last user result for SQLite UserOnlyStore should be null")]
    public void ThenTheLastUserResultForUserOnlyStoreShouldBeNull() =>
        AssertLastUserIsNull();

    [Then("the last users result for SQLite UserOnlyStore should match")]
    public void ThenTheLastUsersResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastUsersMatch(table);

    [Then("the last claims result for SQLite UserOnlyStore should match")]
    public void ThenTheLastClaimsResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("the last logins result for SQLite UserOnlyStore should match")]
    public void ThenTheLastLoginsResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastLoginsMatch(table);

    [Then("the last string result for SQLite UserOnlyStore should be {string}")]
    public void ThenTheLastStringResultForUserOnlyStoreShouldBe(string expected) =>
        AssertLastString(expected);

    [Then("the last string result for SQLite UserOnlyStore should be null")]
    public void ThenTheLastStringResultForUserOnlyStoreShouldBeNull() =>
        AssertLastString(expected: null);

    [Then("the last boolean result for SQLite UserOnlyStore should be {string}")]
    public void ThenTheLastBooleanResultForUserOnlyStoreShouldBe(string expected) =>
        AssertLastBoolean(bool.Parse(expected));

    [Then("the last integer result for SQLite UserOnlyStore should be {int}")]
    public void ThenTheLastIntegerResultForUserOnlyStoreShouldBe(int expected) =>
        AssertLastInteger(expected);

    [Then("{string} on SQLite UserOnlyStore should work without normalized and Guid id")]
    public void ThenMethodOnUserOnlyStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }
}
