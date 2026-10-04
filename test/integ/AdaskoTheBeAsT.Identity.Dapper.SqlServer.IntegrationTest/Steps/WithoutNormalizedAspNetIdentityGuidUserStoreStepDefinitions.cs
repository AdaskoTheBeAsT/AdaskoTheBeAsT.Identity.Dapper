using Reqnroll;
using SqlServerUserOnlyStoreBase =
    AdaskoTheBeAsT.Identity.Dapper.DapperUserOnlyStoreBase<
        AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity.ApplicationUser,
        System.Guid,
        AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity.ApplicationUserClaim,
        AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity.ApplicationUserLogin,
        AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity.ApplicationUserToken,
        Microsoft.Data.SqlClient.SqlConnection>;
using SqlServerUserStoreBase =
    AdaskoTheBeAsT.Identity.Dapper.DapperUserStoreBase<
        AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity.ApplicationUser,
        AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity.ApplicationRole,
        System.Guid,
        AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity.ApplicationUserClaim,
        AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity.ApplicationUserRole,
        AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity.ApplicationUserLogin,
        AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity.ApplicationUserToken,
        Microsoft.Data.SqlClient.SqlConnection>;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Steps;

[Binding]
public sealed class WithoutNormalizedAspNetIdentityGuidUserStoreStepDefinitions
    : SqlServerUserStoreTableDrivenStepDefinitionsBase
{
    public WithoutNormalizedAspNetIdentityGuidUserStoreStepDefinitions(
        FeatureContext featureContext,
        ScenarioContext scenarioContext)
        : base(featureContext, scenarioContext)
    {
    }

    [Given("I have configured SQL Server UserStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredSqlServerUserStoreWithoutNormalizedAndGuidIdAsync() => ResetUserStoreScenarioAsync();

    [Given("I created users for SQL Server UserStore")]
    public Task GivenICreatedUsersForSqlServerUserStoreAsync(Table table) => CreateUsersFromTableAsync(table);

    [Given("I created roles for SQL Server UserStore")]
    public Task GivenICreatedRolesForSqlServerUserStoreAsync(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added user claims for SQL Server UserStore")]
    public Task GivenIAddedUserClaimsForSqlServerUserStoreAsync(Table table) => AddUserClaimsFromTableAsync(table);

    [Given("I added role claims for SQL Server UserStore")]
    public Task GivenIAddedRoleClaimsForSqlServerUserStoreAsync(Table table) => AddRoleClaimsFromTableAsync(table);

    [Given("I added user logins for SQL Server UserStore")]
    public Task GivenIAddedUserLoginsForSqlServerUserStoreAsync(Table table) => AddUserLoginsFromTableAsync(table);

    [Given("I added user tokens for SQL Server UserStore")]
    public Task GivenIAddedUserTokensForSqlServerUserStoreAsync(Table table) => AddUserTokensFromTableAsync(table);

    [Given("I set authenticator keys for SQL Server UserStore")]
    public Task GivenISetAuthenticatorKeysForSqlServerUserStoreAsync(Table table) => SetAuthenticatorKeysFromTableAsync(table);

    [Given("I replaced recovery codes for SQL Server UserStore")]
    public Task GivenIReplacedRecoveryCodesForSqlServerUserStoreAsync(Table table) => ReplaceRecoveryCodesFromTableAsync(table);

    [Given("I added users to roles for SQL Server UserStore")]
    public Task GivenIAddedUsersToRolesForSqlServerUserStoreAsync(Table table) => AddUsersToRolesFromTableAsync(table);

    [When("I verify {string} on SQL Server UserStore without normalized and Guid id")]
    public async Task WhenIVerifyMethodOnSqlServerUserStoreWithoutNormalizedAndGuidIdAsync(string methodName)
    {
        await VerifyUserStoreMethodAsync(methodName);
        LastVerifiedMethodName = methodName;
    }

    [When("I execute {string} on SQL Server UserStore")]
    public Task WhenIExecuteMethodOnSqlServerUserStoreAsync(string methodName) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table: null);

    [When("I execute {string} on SQL Server UserStore with parameters")]
    public Task WhenIExecuteMethodOnSqlServerUserStoreWithParametersAsync(string methodName, Table table) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table);

    [Then("the last identity result for SQL Server UserStore should be successful")]
    public void ThenTheLastIdentityResultForSqlServerUserStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last user result for SQL Server UserStore should match")]
    public void ThenTheLastUserResultForSqlServerUserStoreShouldMatch(Table table) =>
        AssertLastUserMatches(table);

    [Then("the last user result for SQL Server UserStore should be null")]
    public void ThenTheLastUserResultForSqlServerUserStoreShouldBeNull() =>
        AssertLastUserIsNull();

    [Then("the last users result for SQL Server UserStore should match")]
    public void ThenTheLastUsersResultForSqlServerUserStoreShouldMatch(Table table) =>
        AssertLastUsersMatch(table);

    [Then("the last claims result for SQL Server UserStore should match")]
    public void ThenTheLastClaimsResultForSqlServerUserStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("the last logins result for SQL Server UserStore should match")]
    public void ThenTheLastLoginsResultForSqlServerUserStoreShouldMatch(Table table) =>
        AssertLastLoginsMatch(table);

    [Then("the last strings result for SQL Server UserStore should match")]
    public void ThenTheLastStringsResultForSqlServerUserStoreShouldMatch(Table table) =>
        AssertLastStringsMatch(table);

    [Then("the last string result for SQL Server UserStore should be {string}")]
    public void ThenTheLastStringResultForSqlServerUserStoreShouldBe(string expected) =>
        AssertLastString(expected);

    [Then("the last string result for SQL Server UserStore should be null")]
    public void ThenTheLastStringResultForSqlServerUserStoreShouldBeNull() =>
        AssertLastString(expected: null);

    [Then("the last boolean result for SQL Server UserStore should be {string}")]
    public void ThenTheLastBooleanResultForSqlServerUserStoreShouldBe(string expected) =>
        AssertLastBoolean(bool.Parse(expected));

    [Then("the last integer result for SQL Server UserStore should be {int}")]
    public void ThenTheLastIntegerResultForSqlServerUserStoreShouldBe(int expected) =>
        AssertLastInteger(expected);

    [Then("{string} on SQL Server UserStore should work without normalized and Guid id")]
    public void ThenMethodOnSqlServerUserStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }

    protected override SqlServerUserOnlyStoreBase CreateUserStoreInstance() => CreateUserStore();

    protected override SqlServerUserStoreBase CreateRoleCapableUserStoreInstance() => CreateUserStore();
}
