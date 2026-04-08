using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Reqnroll;

namespace AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;

public abstract class GuidRoleStoreTableDrivenStepDefinitionsBase<
    TUser,
    TRole,
    TUserClaim,
    TUserRole,
    TUserLogin,
    TUserToken,
    TRoleClaim,
    TDbConnection>
    : GuidStoreIntegrationTestBase<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TDbConnection>
    where TUser : IdentityUser<Guid>, new()
    where TRole : IdentityRole<Guid>, new()
    where TUserClaim : IdentityUserClaim<Guid>, new()
    where TUserRole : IdentityUserRole<Guid>, new()
    where TUserLogin : IdentityUserLogin<Guid>, new()
    where TUserToken : IdentityUserToken<Guid>, new()
    where TRoleClaim : IdentityRoleClaim<Guid>, new()
    where TDbConnection : IDbConnection
{
    private readonly string _scenarioKey;
    private readonly IDictionary<string, TRole> _rolesByName =
        new Dictionary<string, TRole>(StringComparer.OrdinalIgnoreCase);

    protected GuidRoleStoreTableDrivenStepDefinitionsBase(
        FeatureContext featureContext,
        ScenarioContext scenarioContext,
        Func<DapperRoleStoreBase<TRole, Guid, TRoleClaim, TDbConnection>> roleStoreFactory,
        Func<DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>> userOnlyStoreFactory,
        Func<DapperUserStoreBase<TUser, TRole, Guid, TUserClaim, TUserRole, TUserLogin, TUserToken, TDbConnection>> userStoreFactory)
        : base(roleStoreFactory, userOnlyStoreFactory, userStoreFactory)
    {
        _scenarioKey = BuildScenarioKey($"{featureContext.FeatureInfo.Title}::{scenarioContext.ScenarioInfo.Title}");
    }

    protected string? LastVerifiedMethodName { get; set; }

    protected IdentityResult? LastIdentityResult { get; set; }

    protected TRole? LastRole { get; set; }

    protected IList<TRole> LastRoles { get; set; } = new List<TRole>();

    protected IList<Claim> LastClaims { get; set; } = new List<Claim>();

    protected Task ResetRoleStoreScenarioAsync()
    {
        _rolesByName.Clear();
        LastVerifiedMethodName = null;
        LastIdentityResult = null;
        LastRole = null;
        LastRoles = new List<TRole>();
        LastClaims = new List<Claim>();
        return Task.CompletedTask;
    }

    protected async Task CreateRolesFromTableAsync(Table table)
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

    protected async Task AddRoleClaimsFromTableAsync(Table table)
    {
        using var store = CreateRoleStore();
        foreach (var row in table.Rows)
        {
            var role = GetRole(table, row);
            await store.AddClaimAsync(
                role,
                new Claim(
                    GetRequiredValue(table, row, "ClaimType"),
                    MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValue"))),
                CancellationToken.None);
        }
    }

    protected async Task VerifyRoleStoreMethodAndRememberAsync(string methodName)
    {
        await VerifyRoleStoreMethodAsync(methodName);
        LastVerifiedMethodName = methodName;
    }

    protected Task ExecuteRoleStoreMethodAsync(string methodName)
    {
        using var store = CreateRoleStore();
        switch (methodName)
        {
            case "Roles":
                LastRoles = store.Roles.ToList();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(methodName), methodName, message: null);
        }

        return Task.CompletedTask;
    }

    protected async Task ExecuteRoleStoreMethodAsync(string methodName, Table table)
    {
        using var store = CreateRoleStore();
        var row = table.Rows.Count == 1 ? table.Rows[0] : null;
        switch (methodName)
        {
            case "CreateAsync":
            {
                var role = CreateRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                role.Name = MakeScenarioUniqueValue(role.Name);
                LastIdentityResult = await store.CreateAsync(role, CancellationToken.None);
                _rolesByName[role.Name!] = role;
                LastRole = await store.FindByIdAsync(role.Id.ToString(), CancellationToken.None);
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
                LastIdentityResult = await store.UpdateAsync(existingRole, CancellationToken.None);
                if (!string.Equals(previousName, existingRole.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _rolesByName.Remove(previousName);
                }

                _rolesByName[existingRole.Name!] = existingRole;
                LastRole = await store.FindByIdAsync(existingRole.Id.ToString(), CancellationToken.None);
                break;
            }
            case "DeleteAsync":
            {
                var role = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                LastIdentityResult = await store.DeleteAsync(role, CancellationToken.None);
                _rolesByName.Remove(role.Name!);
                LastRole = await store.FindByIdAsync(role.Id.ToString(), CancellationToken.None);
                break;
            }
            case "FindByIdAsync":
            {
                var role = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                LastRole = await store.FindByIdAsync(role.Id.ToString(), CancellationToken.None);
                break;
            }
            case "FindByNameAsync":
            {
                LastRole = await store.FindByNameAsync(
                    MakeScenarioUniqueValue(
                        GetRequiredValue(table, row ?? throw new InvalidOperationException("Exactly one row is required."), "RoleName")),
                    CancellationToken.None);
                break;
            }
            case "GetClaimsAsync":
            {
                var role = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                LastClaims = await store.GetClaimsAsync(role, CancellationToken.None);
                break;
            }
            case "AddClaimAsync":
            {
                var role = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                await store.AddClaimAsync(
                    role,
                    new Claim(
                        GetRequiredValue(table, row, "ClaimType"),
                        MakeScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValue"))),
                    CancellationToken.None);
                LastClaims = await store.GetClaimsAsync(role, CancellationToken.None);
                break;
            }
            case "RemoveClaimAsync":
            {
                var role = GetRole(table, row ?? throw new InvalidOperationException("Exactly one row is required."));
                await store.RemoveClaimAsync(
                    role,
                    new Claim(
                        GetRequiredValue(table, row, "ClaimType"),
                        MakeScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValue"))),
                    CancellationToken.None);
                LastClaims = await store.GetClaimsAsync(role, CancellationToken.None);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(methodName), methodName, message: null);
        }
    }

    protected void AssertLastIdentityResultSuccessful()
    {
        LastIdentityResult.Should().NotBeNull();
        LastIdentityResult!.Succeeded.Should().BeTrue();
        LastIdentityResult.Errors.Should().BeEmpty();
    }

    protected void AssertLastRoleMatches(Table table)
    {
        LastRole.Should().NotBeNull();
        var row = table.Rows.Count == 1 ? table.Rows[0] : throw new InvalidOperationException("Exactly one row is required.");
        foreach (var header in table.Header)
        {
            switch (header)
            {
                case "Id":
                    LastRole!.Id.ToString().Should().Be(row[header]);
                    break;
                case "Name":
                    LastRole!.Name.Should().Be(MakeScenarioUniqueValue(row[header]));
                    break;
                default:
                    break;
            }
        }
    }

    protected void AssertLastRoleIsNull() => LastRole.Should().BeNull();

    protected void AssertLastRolesMatch(Table table)
    {
        foreach (var row in table.Rows)
        {
            var expectedName = MakeScenarioUniqueValue(GetRequiredValue(table, row, "Name"));
            LastRoles.Should().ContainSingle(
                role => string.Equals(role.Name, expectedName, StringComparison.OrdinalIgnoreCase));
        }
    }

    protected void AssertLastClaimsMatch(Table table)
    {
        var expected = table.Rows.Select(
            row => (
                GetRequiredValue(table, row, "ClaimType"),
                MakeScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValue"))));
        var actual = LastClaims.Select(claim => (claim.Type, claim.Value));
        actual.Should().BeEquivalentTo(expected);
    }

    protected void AssertLastVerifiedMethod(string methodName) => LastVerifiedMethodName.Should().Be(methodName);

    private TRole GetRole(Table table, DataTableRow row)
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

    private string MakeRequiredScenarioUniqueValue(string value) => MakeScenarioUniqueValue(value);
}
