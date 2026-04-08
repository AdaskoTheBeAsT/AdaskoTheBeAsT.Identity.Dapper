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
public sealed class WithoutNormalizedAspNetIdentityGuidUserOnlyStoreStepDefinitions
    : SqlServerUserStoreTableDrivenStepDefinitionsBase
{
    public WithoutNormalizedAspNetIdentityGuidUserOnlyStoreStepDefinitions(
        FeatureContext featureContext,
        ScenarioContext scenarioContext)
        : base(featureContext, scenarioContext)
    {
    }

    protected override SqlServerUserOnlyStoreBase CreateUserStoreInstance() => CreateUserOnlyStore();

    protected override SqlServerUserStoreBase CreateRoleCapableUserStoreInstance() => CreateUserStore();

    [Given("I have configured SQL Server UserOnlyStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredSqlServerUserOnlyStoreWithoutNormalizedAndGuidId() => ResetUserStoreScenarioAsync();

    [Given("I created users for SQL Server UserOnlyStore")]
    public Task GivenICreatedUsersForSqlServerUserOnlyStore(Table table) => CreateUsersFromTableAsync(table);

    [Given("I created roles for SQL Server UserOnlyStore")]
    public Task GivenICreatedRolesForSqlServerUserOnlyStore(Table table) => CreateRolesFromTableAsync(table);

    [Given("I added user claims for SQL Server UserOnlyStore")]
    public Task GivenIAddedUserClaimsForSqlServerUserOnlyStore(Table table) => AddUserClaimsFromTableAsync(table);

    [Given("I added user logins for SQL Server UserOnlyStore")]
    public Task GivenIAddedUserLoginsForSqlServerUserOnlyStore(Table table) => AddUserLoginsFromTableAsync(table);

    [Given("I added user tokens for SQL Server UserOnlyStore")]
    public Task GivenIAddedUserTokensForSqlServerUserOnlyStore(Table table) => AddUserTokensFromTableAsync(table);

    [Given("I set authenticator keys for SQL Server UserOnlyStore")]
    public Task GivenISetAuthenticatorKeysForSqlServerUserOnlyStore(Table table) => SetAuthenticatorKeysFromTableAsync(table);

    [Given("I replaced recovery codes for SQL Server UserOnlyStore")]
    public Task GivenIReplacedRecoveryCodesForSqlServerUserOnlyStore(Table table) => ReplaceRecoveryCodesFromTableAsync(table);

    [Given("I added users to roles for SQL Server UserOnlyStore")]
    public Task GivenIAddedUsersToRolesForSqlServerUserOnlyStore(Table table) => AddUsersToRolesFromTableAsync(table);

    [When("I verify {string} on SQL Server UserOnlyStore without normalized and Guid id")]
    public async Task WhenIVerifyMethodOnSqlServerUserOnlyStoreWithoutNormalizedAndGuidId(string methodName)
    {
        await VerifyUserOnlyStoreMethodAsync(methodName);
        LastVerifiedMethodName = methodName;
    }

    [When("I execute {string} on SQL Server UserOnlyStore")]
    public Task WhenIExecuteMethodOnSqlServerUserOnlyStore(string methodName) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table: null);

    [When("I execute {string} on SQL Server UserOnlyStore with parameters")]
    public Task WhenIExecuteMethodOnSqlServerUserOnlyStoreWithParameters(string methodName, Table table) =>
        ExecuteUserStoreDatabaseMethodAsync(methodName, table);

    [Then("the last identity result for SQL Server UserOnlyStore should be successful")]
    public void ThenTheLastIdentityResultForSqlServerUserOnlyStoreShouldBeSuccessful() =>
        AssertLastIdentityResultSuccessful();

    [Then("the last user result for SQL Server UserOnlyStore should match")]
    public void ThenTheLastUserResultForSqlServerUserOnlyStoreShouldMatch(Table table) =>
        AssertLastUserMatches(table);

    [Then("the last user result for SQL Server UserOnlyStore should be null")]
    public void ThenTheLastUserResultForSqlServerUserOnlyStoreShouldBeNull() =>
        AssertLastUserIsNull();

    [Then("the last users result for SQL Server UserOnlyStore should match")]
    public void ThenTheLastUsersResultForSqlServerUserOnlyStoreShouldMatch(Table table) =>
        AssertLastUsersMatch(table);

    [Then("the last claims result for SQL Server UserOnlyStore should match")]
    public void ThenTheLastClaimsResultForSqlServerUserOnlyStoreShouldMatch(Table table) =>
        AssertLastClaimsMatch(table);

    [Then("the last logins result for SQL Server UserOnlyStore should match")]
    public void ThenTheLastLoginsResultForSqlServerUserOnlyStoreShouldMatch(Table table) =>
        AssertLastLoginsMatch(table);

    [Then("the last string result for SQL Server UserOnlyStore should be {string}")]
    public void ThenTheLastStringResultForSqlServerUserOnlyStoreShouldBe(string expected) =>
        AssertLastString(expected);

    [Then("the last string result for SQL Server UserOnlyStore should be null")]
    public void ThenTheLastStringResultForSqlServerUserOnlyStoreShouldBeNull() =>
        AssertLastString(expected: null);

    [Then("the last boolean result for SQL Server UserOnlyStore should be {string}")]
    public void ThenTheLastBooleanResultForSqlServerUserOnlyStoreShouldBe(string expected) =>
        AssertLastBoolean(bool.Parse(expected));

    [Then("the last integer result for SQL Server UserOnlyStore should be {int}")]
    public void ThenTheLastIntegerResultForSqlServerUserOnlyStoreShouldBe(int expected) =>
        AssertLastInteger(expected);

    [Then("{string} on SQL Server UserOnlyStore should work without normalized and Guid id")]
    public void ThenMethodOnSqlServerUserOnlyStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        AssertLastVerifiedMethod(methodName);
    }
}
