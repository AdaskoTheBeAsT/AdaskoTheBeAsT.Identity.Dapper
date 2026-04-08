using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.Identity;
using Reqnroll;
using Npgsql;
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
public sealed class WithoutNormalizedAspNetIdentityGuidUserStoreStepDefinitions
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

    protected override PostgreSqlUserOnlyStoreBase CreateUserStoreInstance() => CreateUserStore();

    protected override PostgreSqlUserStoreBase CreateRoleCapableUserStoreInstance() => CreateUserStore();

    [Given("I have configured PostgreSQL UserStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredUserStoreWithoutNormalizedAndGuidId() => ResetUserStoreScenarioAsync();

    [Given("I created users for PostgreSQL UserStore")]
    public Task GivenICreatedUsersForUserStore(Table table) => CreateUsersFromTableAsync(table);

    [Given("I created roles for PostgreSQL UserStore")]
    public Task GivenICreatedRolesForUserStore(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added user claims for PostgreSQL UserStore")]
    public Task GivenIAddedUserClaimsForUserStore(Table table) => AddUserClaimsFromTableAsync(table);

    [Given("I added role claims for PostgreSQL UserStore")]
    public Task GivenIAddedRoleClaimsForUserStore(Table table) => AddRoleClaimsFromTableAsync(table);

    [Given("I added user logins for PostgreSQL UserStore")]
    public Task GivenIAddedUserLoginsForUserStore(Table table) => AddUserLoginsFromTableAsync(table);

    [Given("I added user tokens for PostgreSQL UserStore")]
    public Task GivenIAddedUserTokensForUserStore(Table table) => AddUserTokensFromTableAsync(table);

    [Given("I set authenticator keys for PostgreSQL UserStore")]
    public Task GivenISetAuthenticatorKeysForUserStore(Table table) => SetAuthenticatorKeysFromTableAsync(table);

    [Given("I replaced recovery codes for PostgreSQL UserStore")]
    public Task GivenIReplacedRecoveryCodesForUserStore(Table table) => ReplaceRecoveryCodesFromTableAsync(table);

    [Given("I added users to roles for PostgreSQL UserStore")]
    public Task GivenIAddedUsersToRolesForUserStore(Table table) => AddUsersToRolesFromTableAsync(table);

    [When("I verify {string} on PostgreSQL UserStore without normalized and Guid id")]
    public async Task WhenIVerifyMethodOnUserStoreWithoutNormalizedAndGuidId(string methodName)
    {
        await VerifyUserStoreMethodAsync(methodName);
        LastVerifiedMethodName = methodName;
    }

    [When("I execute {string} on PostgreSQL UserStore")]
    public Task WhenIExecuteMethodOnUserStore(string methodName) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table: null);

    [When("I execute {string} on PostgreSQL UserStore with parameters")]
    public Task WhenIExecuteMethodOnUserStoreWithParameters(string methodName, Table table) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table);

    [Then("the last identity result for PostgreSQL UserStore should be successful")]
    public void ThenTheLastIdentityResultForUserStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last user result for PostgreSQL UserStore should match")]
    public void ThenTheLastUserResultForUserStoreShouldMatch(Table table) =>
        AssertLastUserMatches(table);

    [Then("the last user result for PostgreSQL UserStore should be null")]
    public void ThenTheLastUserResultForUserStoreShouldBeNull() =>
        AssertLastUserIsNull();

    [Then("the last users result for PostgreSQL UserStore should match")]
    public void ThenTheLastUsersResultForUserStoreShouldMatch(Table table) =>
        AssertLastUsersMatch(table);

    [Then("the last claims result for PostgreSQL UserStore should match")]
    public void ThenTheLastClaimsResultForUserStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("the last logins result for PostgreSQL UserStore should match")]
    public void ThenTheLastLoginsResultForUserStoreShouldMatch(Table table) =>
        AssertLastLoginsMatch(table);

    [Then("the last strings result for PostgreSQL UserStore should match")]
    public void ThenTheLastStringsResultForUserStoreShouldMatch(Table table) =>
        AssertLastStringsMatch(table);

    [Then("the last string result for PostgreSQL UserStore should be {string}")]
    public void ThenTheLastStringResultForUserStoreShouldBe(string expected) =>
        AssertLastString(expected);

    [Then("the last string result for PostgreSQL UserStore should be null")]
    public void ThenTheLastStringResultForUserStoreShouldBeNull() =>
        AssertLastString(expected: null);

    [Then("the last boolean result for PostgreSQL UserStore should be {string}")]
    public void ThenTheLastBooleanResultForUserStoreShouldBe(string expected) =>
        AssertLastBoolean(bool.Parse(expected));

    [Then("the last integer result for PostgreSQL UserStore should be {int}")]
    public void ThenTheLastIntegerResultForUserStoreShouldBe(int expected) =>
        AssertLastInteger(expected);

    [Then("{string} on PostgreSQL UserStore should work without normalized and Guid id")]
    public void ThenMethodOnUserStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }
}
