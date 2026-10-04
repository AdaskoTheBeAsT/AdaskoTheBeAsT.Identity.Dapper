using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity;
using Npgsql;
using Reqnroll;
using PostgreSqlUserOnlyStoreBase =
    AdaskoTheBeAsT.Identity.Dapper.DapperUserOnlyStoreBase<
        AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity.ApplicationUser,
        System.Guid,
        AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity.ApplicationUserClaim,
        AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity.ApplicationUserLogin,
        AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity.ApplicationUserToken,
        Npgsql.NpgsqlConnection>;
using PostgreSqlUserStoreBase =
    AdaskoTheBeAsT.Identity.Dapper.DapperUserStoreBase<
        AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity.ApplicationUser,
        AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity.ApplicationRole,
        System.Guid,
        AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity.ApplicationUserClaim,
        AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity.ApplicationUserRole,
        AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity.ApplicationUserLogin,
        AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity.ApplicationUserToken,
        Npgsql.NpgsqlConnection>;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Steps;

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
        NpgsqlConnection>
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

    [Given("I have configured PostgreSQL UserOnlyStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredUserOnlyStoreWithoutNormalizedAndGuidIdAsync() => ResetUserStoreScenarioAsync();

    [Given("I created users for PostgreSQL UserOnlyStore")]
    public Task GivenICreatedUsersForUserOnlyStoreAsync(Table table) => CreateUsersFromTableAsync(table);

    [Given("I created roles for PostgreSQL UserOnlyStore")]
    public Task GivenICreatedRolesForUserOnlyStoreAsync(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added user claims for PostgreSQL UserOnlyStore")]
    public Task GivenIAddedUserClaimsForUserOnlyStoreAsync(Table table) => AddUserClaimsFromTableAsync(table);

    [Given("I added user logins for PostgreSQL UserOnlyStore")]
    public Task GivenIAddedUserLoginsForUserOnlyStoreAsync(Table table) => AddUserLoginsFromTableAsync(table);

    [Given("I added user tokens for PostgreSQL UserOnlyStore")]
    public Task GivenIAddedUserTokensForUserOnlyStoreAsync(Table table) => AddUserTokensFromTableAsync(table);

    [Given("I set authenticator keys for PostgreSQL UserOnlyStore")]
    public Task GivenISetAuthenticatorKeysForUserOnlyStoreAsync(Table table) => SetAuthenticatorKeysFromTableAsync(table);

    [Given("I replaced recovery codes for PostgreSQL UserOnlyStore")]
    public Task GivenIReplacedRecoveryCodesForUserOnlyStoreAsync(Table table) => ReplaceRecoveryCodesFromTableAsync(table);

    [Given("I added users to roles for PostgreSQL UserOnlyStore")]
    public Task GivenIAddedUsersToRolesForUserOnlyStoreAsync(Table table) => AddUsersToRolesFromTableAsync(table);

    [When("I verify {string} on PostgreSQL UserOnlyStore without normalized and Guid id")]
    public async Task WhenIVerifyMethodOnUserOnlyStoreWithoutNormalizedAndGuidIdAsync(string methodName)
    {
        await VerifyUserOnlyStoreMethodAsync(methodName);
        LastVerifiedMethodName = methodName;
    }

    [When("I execute {string} on PostgreSQL UserOnlyStore")]
    public Task WhenIExecuteMethodOnUserOnlyStoreAsync(string methodName) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table: null);

    [When("I execute {string} on PostgreSQL UserOnlyStore with parameters")]
    public Task WhenIExecuteMethodOnUserOnlyStoreWithParametersAsync(string methodName, Table table) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table);

    [Then("the last identity result for PostgreSQL UserOnlyStore should be successful")]
    public void ThenTheLastIdentityResultForUserOnlyStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last user result for PostgreSQL UserOnlyStore should match")]
    public void ThenTheLastUserResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastUserMatches(table);

    [Then("the last user result for PostgreSQL UserOnlyStore should be null")]
    public void ThenTheLastUserResultForUserOnlyStoreShouldBeNull() =>
        AssertLastUserIsNull();

    [Then("the last users result for PostgreSQL UserOnlyStore should match")]
    public void ThenTheLastUsersResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastUsersMatch(table);

    [Then("the last claims result for PostgreSQL UserOnlyStore should match")]
    public void ThenTheLastClaimsResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("the last logins result for PostgreSQL UserOnlyStore should match")]
    public void ThenTheLastLoginsResultForUserOnlyStoreShouldMatch(Table table) =>
        AssertLastLoginsMatch(table);

    [Then("the last string result for PostgreSQL UserOnlyStore should be {string}")]
    public void ThenTheLastStringResultForUserOnlyStoreShouldBe(string expected) =>
        AssertLastString(expected);

    [Then("the last string result for PostgreSQL UserOnlyStore should be null")]
    public void ThenTheLastStringResultForUserOnlyStoreShouldBeNull() =>
        AssertLastString(expected: null);

    [Then("the last boolean result for PostgreSQL UserOnlyStore should be {string}")]
    public void ThenTheLastBooleanResultForUserOnlyStoreShouldBe(string expected) =>
        AssertLastBoolean(bool.Parse(expected));

    [Then("the last integer result for PostgreSQL UserOnlyStore should be {int}")]
    public void ThenTheLastIntegerResultForUserOnlyStoreShouldBe(int expected) =>
        AssertLastInteger(expected);

    [Then("{string} on PostgreSQL UserOnlyStore should work without normalized and Guid id")]
    public void ThenMethodOnUserOnlyStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }

    protected override PostgreSqlUserOnlyStoreBase CreateUserStoreInstance() => CreateUserOnlyStore();

    protected override PostgreSqlUserStoreBase CreateRoleCapableUserStoreInstance() => CreateUserStore();
}
