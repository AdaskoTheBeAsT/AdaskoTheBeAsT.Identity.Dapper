using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Reqnroll;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Steps;

[Binding]
public sealed class WithoutNormalizedAspNetIdentityGuidRoleStoreStepDefinitions
    : SqlServerStoreIntegrationTestBase
{
    private readonly string _scenarioKey;
    private readonly IDictionary<string, Identity.ApplicationRole> _rolesByName =
        new Dictionary<string, Identity.ApplicationRole>(StringComparer.OrdinalIgnoreCase);

    private string? _verifiedMethodName;
    private IdentityResult? _lastIdentityResult;
    private Identity.ApplicationRole? _lastRole;
    private IList<Identity.ApplicationRole> _lastRoles = new List<Identity.ApplicationRole>();
    private IList<Claim> _lastClaims = new List<Claim>();

    public WithoutNormalizedAspNetIdentityGuidRoleStoreStepDefinitions(
        FeatureContext featureContext,
        ScenarioContext scenarioContext)
    {
        _scenarioKey = BuildScenarioKey($"{featureContext.FeatureInfo.Title}::{scenarioContext.ScenarioInfo.Title}");
    }

    [Given("I have configured SQL Server RoleStore without normalized and Guid id")]
    public Task GivenIHaveConfiguredSqlServerRoleStoreWithoutNormalizedAndGuidId()
    {
        _rolesByName.Clear();
        _verifiedMethodName = null;
        _lastIdentityResult = null;
        _lastRole = null;
        _lastRoles = new List<Identity.ApplicationRole>();
        _lastClaims = new List<Claim>();
        return Task.CompletedTask;
    }

    [Given("I created roles for SQL Server RoleStore")]
    public async Task GivenICreatedRolesForSqlServerRoleStore(Table table)
    {
        using var store = CreateRoleStore();
        foreach (var row in table.Rows)
        {
            var role = CreateRole(table, row);
            role.Name = MakeScenarioUniqueValue(role.Name);
            var result = await store.CreateAsync(role, CancellationToken.None);
            result.Succeeded.Should().BeTrue();
            _rolesByName[role.Name!] = role;
        }
    }

    [Given("I added role claims for SQL Server RoleStore")]
    public async Task GivenIAddedRoleClaimsForSqlServerRoleStore(Table table)
    {
        using var store = CreateRoleStore();
        foreach (var row in table.Rows)
        {
            var role = GetRole(table, row);
            await store.AddClaimAsync(
                role,
                new Claim(GetRequiredValue(table, row, "ClaimType"), MakeScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValue"))),
                CancellationToken.None);
        }
    }

    [When("I verify {string} on SQL Server RoleStore without normalized and Guid id")]
    public async Task WhenIVerifyMethodOnSqlServerRoleStoreWithoutNormalizedAndGuidId(string methodName)
    {
        await VerifyRoleStoreMethodAsync(methodName);
        _verifiedMethodName = methodName;
    }

    [When("I execute {string} on SQL Server RoleStore")]
    public async Task WhenIExecuteMethodOnSqlServerRoleStore(string methodName)
    {
        using var store = CreateRoleStore();
        switch (methodName)
        {
            case "Roles":
                _lastRoles = store.Roles.ToList();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(methodName), methodName, message: null);
        }
    }

    [When("I execute {string} on SQL Server RoleStore with parameters")]
    public async Task WhenIExecuteMethodOnSqlServerRoleStoreWithParameters(string methodName, Table table)
    {
        using var store = CreateRoleStore();
        var row = table.Rows.Count == 1 ? table.Rows[0] : null;
        switch (methodName)
        {
            case "CreateAsync":
            {
                var role = CreateRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                role.Name = MakeScenarioUniqueValue(role.Name);
                _lastIdentityResult = await store.CreateAsync(role, CancellationToken.None);
                _rolesByName[role.Name!] = role;
                _lastRole = await store.FindByIdAsync(role.Id.ToString(), CancellationToken.None);
                break;
            }
            case "UpdateAsync":
            {
                var existingRole = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                var previousName = existingRole.Name!;
                existingRole.Name = GetOptionalValue(table, row, "NewName") is { } newName
                    ? MakeScenarioUniqueValue(newName)
                    : existingRole.Name;
                existingRole.ConcurrencyStamp = GetOptionalValue(table, row, "NewConcurrencyStamp") ?? existingRole.ConcurrencyStamp;
                _lastIdentityResult = await store.UpdateAsync(existingRole, CancellationToken.None);
                if (!string.Equals(previousName, existingRole.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _rolesByName.Remove(previousName);
                }

                _rolesByName[existingRole.Name!] = existingRole;
                _lastRole = await store.FindByIdAsync(existingRole.Id.ToString(), CancellationToken.None);
                break;
            }
            case "DeleteAsync":
            {
                var role = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                _lastIdentityResult = await store.DeleteAsync(role, CancellationToken.None);
                _rolesByName.Remove(role.Name!);
                _lastRole = await store.FindByIdAsync(role.Id.ToString(), CancellationToken.None);
                break;
            }
            case "FindByIdAsync":
            {
                var role = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                _lastRole = await store.FindByIdAsync(role.Id.ToString(), CancellationToken.None);
                break;
            }
            case "FindByNameAsync":
            {
                _lastRole = await store.FindByNameAsync(
                    MakeScenarioUniqueValue(GetRequiredValue(table, row ?? throw new InvalidOperationException("Exactly one row is required."), "RoleName")),
                    CancellationToken.None);
                break;
            }
            case "GetClaimsAsync":
            {
                var role = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                _lastClaims = await store.GetClaimsAsync(role, CancellationToken.None);
                break;
            }
            case "AddClaimAsync":
            {
                var role = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                await store.AddClaimAsync(
                    role,
                    new Claim(GetRequiredValue(table, row, "ClaimType"), MakeScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValue"))),
                    CancellationToken.None);
                _lastClaims = await store.GetClaimsAsync(role, CancellationToken.None);
                break;
            }
            case "RemoveClaimAsync":
            {
                var role = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                await store.RemoveClaimAsync(
                    role,
                    new Claim(GetRequiredValue(table, row, "ClaimType"), MakeScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValue"))),
                    CancellationToken.None);
                _lastClaims = await store.GetClaimsAsync(role, CancellationToken.None);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(methodName), methodName, message: null);
        }
    }

    [Then("the last identity result for SQL Server RoleStore should be successful")]
    public void ThenTheLastIdentityResultForSqlServerRoleStoreShouldBeSuccessful()
    {
        _lastIdentityResult.Should().NotBeNull();
        _lastIdentityResult!.Succeeded.Should().BeTrue();
        _lastIdentityResult.Errors.Should().BeEmpty();
    }

    [Then("the last role result for SQL Server RoleStore should match")]
    public void ThenTheLastRoleResultForSqlServerRoleStoreShouldMatch(Table table)
    {
        _lastRole.Should().NotBeNull();
        var row = table.Rows.Count == 1 ? table.Rows[0] : throw new InvalidOperationException("Exactly one row is required.");
        foreach (var header in table.Header)
        {
            switch (header)
            {
                case "Id":
                    _lastRole!.Id.ToString().Should().Be(row[header]);
                    break;
                case "Name":
                    _lastRole!.Name.Should().Be(MakeScenarioUniqueValue(row[header]));
                    break;
                default:
                    break;
            }
        }
    }

    [Then("the last role result for SQL Server RoleStore should be null")]
    public void ThenTheLastRoleResultForSqlServerRoleStoreShouldBeNull() => _lastRole.Should().BeNull();

    [Then("the last roles result for SQL Server RoleStore should match")]
    public void ThenTheLastRolesResultForSqlServerRoleStoreShouldMatch(Table table)
    {
        foreach (var row in table.Rows)
        {
            var expectedName = MakeScenarioUniqueValue(GetRequiredValue(table, row, "Name"));
            _lastRoles.Should().ContainSingle(role => string.Equals(role.Name, expectedName, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Then("the last claims result for SQL Server RoleStore should match")]
    public void ThenTheLastClaimsResultForSqlServerRoleStoreShouldMatch(Table table)
    {
        var expected = table.Rows.Select(
            row => (GetRequiredValue(table, row, "ClaimType"), MakeScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValue"))));
        var actual = _lastClaims.Select(claim => (claim.Type, claim.Value));
        actual.Should().BeEquivalentTo(expected);
    }

    [Then("{string} on SQL Server RoleStore should work without normalized and Guid id")]
    public void ThenMethodOnSqlServerRoleStoreShouldWorkWithoutNormalizedAndGuidId(string methodName)
    {
        _verifiedMethodName.Should().Be(methodName);
    }

    private Identity.ApplicationRole GetRole(Table table, DataTableRow row)
    {
        var roleName = MakeScenarioUniqueValue(GetRequiredValue(table, row, "RoleName"));
        return _rolesByName.TryGetValue(roleName, out var role)
            ? role
            : throw new KeyNotFoundException($"Role '{roleName}' was not created in the scenario setup.");
    }

    private static string BuildScenarioKey(string scenarioTitle)
    {
        var sanitized = new string(scenarioTitle.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        var prefix = string.IsNullOrWhiteSpace(sanitized) ? "scenario" : sanitized[..Math.Min(12, sanitized.Length)];
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scenarioTitle)))[..8].ToLowerInvariant();
        return $"{prefix}{hash}";
    }

    private string MakeScenarioUniqueValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? value! : $"{value}-{_scenarioKey}";
}
