using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Reqnroll;

namespace AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;

public abstract class GuidUserStoreTableDrivenStepDefinitionsBase<
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
    private readonly IDictionary<string, TUser> _usersByName =
        new Dictionary<string, TUser>(StringComparer.OrdinalIgnoreCase);

    protected GuidUserStoreTableDrivenStepDefinitionsBase(
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

    protected TUser? LastUser { get; set; }

    protected IList<TUser> LastUsers { get; set; } = new List<TUser>();

    protected IList<Claim> LastClaims { get; set; } = new List<Claim>();

    protected IList<UserLoginInfo> LastLogins { get; set; } = new List<UserLoginInfo>();

    protected IList<string> LastStrings { get; set; } = new List<string>();

    protected string? LastString { get; set; }

    protected bool? LastBoolean { get; set; }

    protected int? LastInteger { get; set; }

    protected abstract DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>
        CreateUserStoreInstance();

    protected virtual DapperUserStoreBase<TUser, TRole, Guid, TUserClaim, TUserRole, TUserLogin, TUserToken, TDbConnection>
        CreateRoleCapableUserStoreInstance() =>
        throw new NotSupportedException("This store does not support role-specific methods.");

    protected Task ResetUserStoreScenarioAsync()
    {
        _usersByName.Clear();
        _rolesByName.Clear();
        LastVerifiedMethodName = null;
        LastIdentityResult = null;
        LastUser = null;
        LastUsers = new List<TUser>();
        LastClaims = new List<Claim>();
        LastLogins = new List<UserLoginInfo>();
        LastStrings = new List<string>();
        LastString = null;
        LastBoolean = null;
        LastInteger = null;
        return Task.CompletedTask;
    }

    protected async Task CreateUsersFromTableAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        foreach (var row in table.Rows)
        {
            var user = CreateUser(table, row);
            user.UserName = MakeScenarioUniqueValue(user.UserName);
            user.Email = user.Email == null ? null : MakeScenarioUniqueEmail(user.Email);
            LastIdentityResult = await store.CreateAsync(user, CancellationToken.None);
            LastIdentityResult.Succeeded.Should().BeTrue();
            _usersByName[user.UserName!] = user;
            LastUser = user;
        }
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

    protected async Task AddUserClaimsFromTableAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        foreach (var row in table.Rows)
        {
            var user = GetUser(table, row);
            var claim = CreateClaim(table, row);
            await store.AddClaimsAsync(user, new[] { claim }, CancellationToken.None);
        }
    }

    protected async Task AddRoleClaimsFromTableAsync(Table table)
    {
        using var store = CreateRoleStore();
        foreach (var row in table.Rows)
        {
            var role = GetRole(table, row);
            var claim = CreateClaim(table, row);
            await store.AddClaimAsync(role, claim, CancellationToken.None);
        }
    }

    protected async Task AddUserLoginsFromTableAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        foreach (var row in table.Rows)
        {
            var user = GetUser(table, row);
            await store.AddLoginAsync(user, CreateLoginInfo(table, row), CancellationToken.None);
        }
    }

    protected async Task AddUserTokensFromTableAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        foreach (var row in table.Rows)
        {
            var user = GetUser(table, row);
            await store.SetTokenAsync(
                user,
                GetRequiredValue(table, row, "LoginProvider"),
                GetRequiredValue(table, row, "Name"),
                MakeScenarioUniqueValue(GetOptionalValue(table, row, "Value")),
                CancellationToken.None);
        }
    }

    protected async Task SetAuthenticatorKeysFromTableAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        foreach (var row in table.Rows)
        {
            var user = GetUser(table, row);
            await store.SetAuthenticatorKeyAsync(
                user,
                MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "Key")),
                CancellationToken.None);
        }
    }

    protected async Task ReplaceRecoveryCodesFromTableAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        foreach (var row in table.Rows)
        {
            var user = GetUser(table, row);
            await store.ReplaceCodesAsync(
                user,
                SplitValues(GetRequiredValue(table, row, "RecoveryCodes")).Select(MakeRequiredScenarioUniqueValue),
                CancellationToken.None);
        }
    }

    protected async Task AddUsersToRolesFromTableAsync(Table table)
    {
        using var store = CreateRoleCapableUserStoreInstance();
        foreach (var row in table.Rows)
        {
            var user = GetUser(table, row);
            var roleName = MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "RoleName"));
            await store.AddToRoleAsync(user, roleName, CancellationToken.None);
        }
    }

    protected async Task ExecuteUserStoreDatabaseMethodAsync(string methodName, Table? table)
    {
        switch (methodName)
        {
            case "Users":
                using (var store = CreateUserStoreInstance())
                {
                    LastUsers = store.Users.ToList();
                }

                break;
            case "CreateAsync":
                await ExecuteCreateUserAsync(RequireTable(table));
                break;
            case "UpdateAsync":
                await ExecuteUpdateUserAsync(RequireTable(table));
                break;
            case "DeleteAsync":
                await ExecuteDeleteUserAsync(RequireTable(table));
                break;
            case "FindByIdAsync":
                await ExecuteFindUserByIdAsync(RequireTable(table));
                break;
            case "FindByNameAsync":
                await ExecuteFindUserByNameAsync(RequireTable(table));
                break;
            case "FindByEmailAsync":
                await ExecuteFindUserByEmailAsync(RequireTable(table));
                break;
            case "GetClaimsAsync":
                await ExecuteGetClaimsAsync(RequireTable(table));
                break;
            case "AddClaimsAsync":
                await ExecuteAddClaimsAsync(RequireTable(table));
                break;
            case "ReplaceClaimAsync":
                await ExecuteReplaceClaimAsync(RequireTable(table));
                break;
            case "RemoveClaimsAsync":
                await ExecuteRemoveClaimsAsync(RequireTable(table));
                break;
            case "AddLoginAsync":
                await ExecuteAddLoginAsync(RequireTable(table));
                break;
            case "RemoveLoginAsync":
                await ExecuteRemoveLoginAsync(RequireTable(table));
                break;
            case "GetLoginsAsync":
                await ExecuteGetLoginsAsync(RequireTable(table));
                break;
            case "FindByLoginAsync":
                await ExecuteFindByLoginAsync(RequireTable(table));
                break;
            case "IncrementAccessFailedCountAsync":
                await ExecuteIncrementAccessFailedCountAsync(RequireTable(table));
                break;
            case "GetUsersForClaimAsync":
                await ExecuteGetUsersForClaimAsync(RequireTable(table));
                break;
            case "SetTokenAsync":
                await ExecuteSetTokenAsync(RequireTable(table));
                break;
            case "RemoveTokenAsync":
                await ExecuteRemoveTokenAsync(RequireTable(table));
                break;
            case "GetTokenAsync":
                await ExecuteGetTokenAsync(RequireTable(table));
                break;
            case "SetAuthenticatorKeyAsync":
                await ExecuteSetAuthenticatorKeyAsync(RequireTable(table));
                break;
            case "GetAuthenticatorKeyAsync":
                await ExecuteGetAuthenticatorKeyAsync(RequireTable(table));
                break;
            case "CountCodesAsync":
                await ExecuteCountCodesAsync(RequireTable(table));
                break;
            case "ReplaceCodesAsync":
                await ExecuteReplaceCodesAsync(RequireTable(table));
                break;
            case "RedeemCodeAsync":
                await ExecuteRedeemCodeAsync(RequireTable(table));
                break;
            case "GetUsersInRoleAsync":
                await ExecuteGetUsersInRoleAsync(RequireTable(table));
                break;
            case "AddToRoleAsync":
                await ExecuteAddToRoleAsync(RequireTable(table));
                break;
            case "RemoveFromRoleAsync":
                await ExecuteRemoveFromRoleAsync(RequireTable(table));
                break;
            case "GetRolesAsync":
                await ExecuteGetRolesAsync(RequireTable(table));
                break;
            case "IsInRoleAsync":
                await ExecuteIsInRoleAsync(RequireTable(table));
                break;
            case "GetRoleClaimsAsync":
                await ExecuteGetRoleClaimsAsync(RequireTable(table));
                break;
            case "GetUserAndRoleClaimsAsync":
                await ExecuteGetUserAndRoleClaimsAsync(RequireTable(table));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(methodName), methodName, message: null);
        }

        LastVerifiedMethodName = methodName;
    }

    protected void AssertLastVerifiedMethod(string methodName) => LastVerifiedMethodName.Should().Be(methodName);

    protected void AssertLastIdentityResultSuccessful()
    {
        LastIdentityResult.Should().NotBeNull();
        LastIdentityResult!.Succeeded.Should().BeTrue();
        LastIdentityResult.Errors.Should().BeEmpty();
    }

    protected void AssertLastUserMatches(Table table)
    {
        LastUser.Should().NotBeNull();
        var row = RequireSingleRow(table);
        AssertUserMatches(LastUser!, table, row);
    }

    protected void AssertLastUserIsNull() => LastUser.Should().BeNull();

    protected void AssertLastUsersMatch(Table table)
    {
        foreach (var row in table.Rows)
        {
            var actual = ResolveUserFromCollection(LastUsers, table, row);
            AssertUserMatches(actual, table, row);
        }
    }

    protected void AssertLastClaimsMatch(Table table)
    {
        var expected = table.Rows.Select(
            row => (
                GetRequiredValue(table, row, "ClaimType"),
                MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValue"))));
        var actual = LastClaims.Select(claim => (claim.Type, claim.Value));
        actual.Should().BeEquivalentTo(expected);
    }

    protected void AssertLastLoginsMatch(Table table)
    {
        var expected = table.Rows.Select(
            row => (
                GetRequiredValue(table, row, "LoginProvider"),
                MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "ProviderKey")),
                GetOptionalValue(table, row, "ProviderDisplayName")));
        var actual = LastLogins.Select(login => (login.LoginProvider, login.ProviderKey, login.ProviderDisplayName));
        actual.Should().BeEquivalentTo(expected);
    }

    protected void AssertLastStringsMatch(Table table)
    {
        var header = table.Header.FirstOrDefault() ?? "Value";
        var expected = table.Rows.Select(row => MakeScenarioUniqueValue(row[header]));
        LastStrings.Should().BeEquivalentTo(expected);
    }

    protected void AssertLastString(string? expected) => LastString.Should().Be(MakeScenarioUniqueValue(expected));

    protected void AssertLastBoolean(bool expected)
    {
        LastBoolean.Should().NotBeNull();
        LastBoolean.Should().Be(expected);
    }

    protected void AssertLastInteger(int expected)
    {
        LastInteger.Should().NotBeNull();
        LastInteger.Should().Be(expected);
    }

    private async Task ExecuteCreateUserAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = CreateUser(table, row);
        user.UserName = MakeScenarioUniqueValue(user.UserName);
        user.Email = user.Email == null ? null : MakeScenarioUniqueEmail(user.Email);
        LastIdentityResult = await store.CreateAsync(user, CancellationToken.None);
        _usersByName[user.UserName!] = user;
        LastUser = await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None);
    }

    private async Task ExecuteUpdateUserAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        var previousUserName = user.UserName!;
        ApplyUserUpdates(table, row, user);
        LastIdentityResult = await store.UpdateAsync(user, CancellationToken.None);
        if (!string.Equals(previousUserName, user.UserName, StringComparison.OrdinalIgnoreCase))
        {
            _usersByName.Remove(previousUserName);
        }

        _usersByName[user.UserName!] = user;
        LastUser = await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None);
    }

    private async Task ExecuteDeleteUserAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        LastIdentityResult = await store.DeleteAsync(user, CancellationToken.None);
        _usersByName.Remove(user.UserName!);
        LastUser = await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None);
    }

    private async Task ExecuteFindUserByIdAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var user = GetUser(table, RequireSingleRow(table));
        LastUser = await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None);
    }

    private async Task ExecuteFindUserByNameAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        LastUser = await store.FindByNameAsync(
            MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "UserName")),
            CancellationToken.None);
    }

    private async Task ExecuteFindUserByEmailAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        LastUser = await store.FindByEmailAsync(
            MakeScenarioUniqueEmail(GetRequiredValue(table, row, "Email")),
            CancellationToken.None);
    }

    private async Task ExecuteGetClaimsAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        LastClaims = await store.GetClaimsAsync(GetUser(table, RequireSingleRow(table)), CancellationToken.None);
    }

    private async Task ExecuteAddClaimsAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = table.Rows.Count >= 1 ? table.Rows[0] : throw new InvalidOperationException("At least one row is expected.");
        var user = GetUser(table, row);
        var claims = table.Rows.Select(r => CreateClaim(table, r)).ToList();
        await store.AddClaimsAsync(user, claims, CancellationToken.None);
        LastClaims = await store.GetClaimsAsync(user, CancellationToken.None);
    }

    private async Task ExecuteReplaceClaimAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        await store.ReplaceClaimAsync(
            user,
            new Claim(
                GetRequiredValue(table, row, "ClaimTypeOld"),
                MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValueOld"))),
            new Claim(
                GetRequiredValue(table, row, "ClaimTypeNew"),
                MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValueNew"))),
            CancellationToken.None);
        LastClaims = await store.GetClaimsAsync(user, CancellationToken.None);
    }

    private async Task ExecuteRemoveClaimsAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        var claims = table.Rows.Select(r => CreateClaim(table, r)).ToList();
        await store.RemoveClaimsAsync(user, claims, CancellationToken.None);
        LastClaims = await store.GetClaimsAsync(user, CancellationToken.None);
    }

    private async Task ExecuteAddLoginAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        await store.AddLoginAsync(user, CreateLoginInfo(table, row), CancellationToken.None);
        LastLogins = await store.GetLoginsAsync(user, CancellationToken.None);
    }

    private async Task ExecuteRemoveLoginAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        await store.RemoveLoginAsync(
            user,
            GetRequiredValue(table, row, "LoginProvider"),
            MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "ProviderKey")),
            CancellationToken.None);
        LastLogins = await store.GetLoginsAsync(user, CancellationToken.None);
    }

    private async Task ExecuteGetLoginsAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        LastLogins = await store.GetLoginsAsync(GetUser(table, RequireSingleRow(table)), CancellationToken.None);
    }

    private async Task ExecuteFindByLoginAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        LastUser = await store.FindByLoginAsync(
            GetRequiredValue(table, row, "LoginProvider"),
            MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "ProviderKey")),
            CancellationToken.None);
    }

    private async Task ExecuteIncrementAccessFailedCountAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var user = GetUser(table, RequireSingleRow(table));
        LastInteger = await store.IncrementAccessFailedCountAsync(user, CancellationToken.None);
        LastUser = await store.FindByIdAsync(user.Id.ToString(), CancellationToken.None);
    }

    private async Task ExecuteGetUsersForClaimAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        LastUsers = await store.GetUsersForClaimAsync(CreateClaim(table, row), CancellationToken.None);
    }

    private async Task ExecuteSetTokenAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        var loginProvider = GetRequiredValue(table, row, "LoginProvider");
        var name = GetRequiredValue(table, row, "Name");
        await store.SetTokenAsync(
            user,
            loginProvider,
            name,
            MakeScenarioUniqueValue(GetOptionalValue(table, row, "Value")),
            CancellationToken.None);
        LastString = await store.GetTokenAsync(user, loginProvider, name, CancellationToken.None);
    }

    private async Task ExecuteRemoveTokenAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        var loginProvider = GetRequiredValue(table, row, "LoginProvider");
        var name = GetRequiredValue(table, row, "Name");
        await store.RemoveTokenAsync(user, loginProvider, name, CancellationToken.None);
        LastString = await store.GetTokenAsync(user, loginProvider, name, CancellationToken.None);
    }

    private async Task ExecuteGetTokenAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        LastString = await store.GetTokenAsync(
            GetUser(table, row),
            GetRequiredValue(table, row, "LoginProvider"),
            GetRequiredValue(table, row, "Name"),
            CancellationToken.None);
    }

    private async Task ExecuteSetAuthenticatorKeyAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        await store.SetAuthenticatorKeyAsync(
            user,
            MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "Key")),
            CancellationToken.None);
        LastString = await store.GetAuthenticatorKeyAsync(user, CancellationToken.None);
    }

    private async Task ExecuteGetAuthenticatorKeyAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        LastString = await store.GetAuthenticatorKeyAsync(GetUser(table, RequireSingleRow(table)), CancellationToken.None);
    }

    private async Task ExecuteCountCodesAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        LastInteger = await store.CountCodesAsync(GetUser(table, RequireSingleRow(table)), CancellationToken.None);
    }

    private async Task ExecuteReplaceCodesAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        await store.ReplaceCodesAsync(
            user,
            SplitValues(GetRequiredValue(table, row, "RecoveryCodes")).Select(MakeRequiredScenarioUniqueValue),
            CancellationToken.None);
        LastInteger = await store.CountCodesAsync(user, CancellationToken.None);
    }

    private async Task ExecuteRedeemCodeAsync(Table table)
    {
        using var store = CreateUserStoreInstance();
        var row = RequireSingleRow(table);
        LastBoolean = await store.RedeemCodeAsync(
            GetUser(table, row),
            MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "Code")),
            CancellationToken.None);
    }

    private async Task ExecuteGetUsersInRoleAsync(Table table)
    {
        using var store = CreateRoleCapableUserStoreInstance();
        var row = RequireSingleRow(table);
        LastUsers = await store.GetUsersInRoleAsync(
            MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "RoleName")),
            CancellationToken.None);
    }

    private async Task ExecuteAddToRoleAsync(Table table)
    {
        using var store = CreateRoleCapableUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        var roleName = MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "RoleName"));
        await store.AddToRoleAsync(user, roleName, CancellationToken.None);
        LastStrings = await store.GetRolesAsync(user, CancellationToken.None);
    }

    private async Task ExecuteRemoveFromRoleAsync(Table table)
    {
        using var store = CreateRoleCapableUserStoreInstance();
        var row = RequireSingleRow(table);
        var user = GetUser(table, row);
        var roleName = MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "RoleName"));
        await store.RemoveFromRoleAsync(user, roleName, CancellationToken.None);
        LastStrings = await store.GetRolesAsync(user, CancellationToken.None);
    }

    private async Task ExecuteGetRolesAsync(Table table)
    {
        using var store = CreateRoleCapableUserStoreInstance();
        LastStrings = await store.GetRolesAsync(GetUser(table, RequireSingleRow(table)), CancellationToken.None);
    }

    private async Task ExecuteIsInRoleAsync(Table table)
    {
        using var store = CreateRoleCapableUserStoreInstance();
        var row = RequireSingleRow(table);
        LastBoolean = await store.IsInRoleAsync(
            GetUser(table, row),
            MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "RoleName")),
            CancellationToken.None);
    }

    private async Task ExecuteGetRoleClaimsAsync(Table table)
    {
        using var store = CreateRoleCapableUserStoreInstance();
        LastClaims = await store.GetRoleClaimsAsync(GetUser(table, RequireSingleRow(table)), CancellationToken.None);
    }

    private async Task ExecuteGetUserAndRoleClaimsAsync(Table table)
    {
        using var store = CreateRoleCapableUserStoreInstance();
        LastClaims = await store.GetUserAndRoleClaimsAsync(GetUser(table, RequireSingleRow(table)), CancellationToken.None);
    }

    private static Table RequireTable(Table? table) => table ?? throw new InvalidOperationException("A data table is required.");

    private static DataTableRow RequireSingleRow(Table table) =>
        table.Rows.Count == 1 ? table.Rows[0] : throw new InvalidOperationException("Exactly one row is expected.");

    private TUser GetUser(Table table, DataTableRow row)
    {
        var userName = MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "UserName"));
        return _usersByName.TryGetValue(userName, out var user)
            ? user
            : throw new KeyNotFoundException($"User '{userName}' was not created in the scenario setup.");
    }

    private TRole GetRole(Table table, DataTableRow row)
    {
        var roleName = MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "RoleName"));
        return _rolesByName.TryGetValue(roleName, out var role)
            ? role
            : throw new KeyNotFoundException($"Role '{roleName}' was not created in the scenario setup.");
    }

    private void ApplyUserUpdates(Table table, DataTableRow row, TUser user)
    {
        user.UserName = GetOptionalValue(table, row, "NewUserName") is { } newUserName
            ? MakeScenarioUniqueValue(newUserName)
            : user.UserName;
        user.Email = GetOptionalValue(table, row, "NewEmail") is { } newEmail
            ? MakeScenarioUniqueEmail(newEmail)
            : user.Email;
        user.PasswordHash = GetOptionalValue(table, row, "NewPasswordHash") ?? user.PasswordHash;
        user.PhoneNumber = GetOptionalValue(table, row, "NewPhoneNumber") ?? user.PhoneNumber;
        user.SecurityStamp = GetOptionalValue(table, row, "NewSecurityStamp") ?? user.SecurityStamp;
        user.EmailConfirmed = GetBoolValue(table, row, "NewEmailConfirmed") ?? user.EmailConfirmed;
        user.PhoneNumberConfirmed = GetBoolValue(table, row, "NewPhoneNumberConfirmed") ?? user.PhoneNumberConfirmed;
        user.TwoFactorEnabled = GetBoolValue(table, row, "NewTwoFactorEnabled") ?? user.TwoFactorEnabled;
        user.LockoutEnabled = GetBoolValue(table, row, "NewLockoutEnabled") ?? user.LockoutEnabled;
        user.AccessFailedCount = GetIntValue(table, row, "NewAccessFailedCount") ?? user.AccessFailedCount;
        user.LockoutEnd = GetDateTimeOffsetValue(table, row, "NewLockoutEnd") ?? user.LockoutEnd;
    }

    private Claim CreateClaim(Table table, DataTableRow row) =>
        new(
            GetRequiredValue(table, row, "ClaimType"),
            MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "ClaimValue")));

    private UserLoginInfo CreateLoginInfo(Table table, DataTableRow row) =>
        new(
            GetRequiredValue(table, row, "LoginProvider"),
            MakeRequiredScenarioUniqueValue(GetRequiredValue(table, row, "ProviderKey")),
            GetOptionalValue(table, row, "ProviderDisplayName"));

    private static IEnumerable<string> SplitValues(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private TUser ResolveUserFromCollection(IEnumerable<TUser> users, Table table, DataTableRow row)
    {
        if (GetOptionalValue(table, row, "Id") is { } idText)
        {
            var id = Guid.Parse(idText);
            return users.Single(user => user.Id == id);
        }

        if (GetOptionalValue(table, row, "UserName") is { } userName)
        {
            var scenarioUserName = MakeRequiredScenarioUniqueValue(userName);
            return users.Single(
                user => string.Equals(user.UserName, scenarioUserName, StringComparison.OrdinalIgnoreCase));
        }

        return users.Single();
    }

    private void AssertUserMatches(TUser actual, Table table, DataTableRow row)
    {
        foreach (var header in table.Header)
        {
            switch (header)
            {
                case "Id":
                    actual.Id.ToString().Should().Be(row[header]);
                    break;
                case "UserName":
                    actual.UserName.Should().Be(MakeRequiredScenarioUniqueValue(row[header]));
                    break;
                case "Email":
                    actual.Email.Should().Be(MakeScenarioUniqueEmail(row[header]));
                    break;
                case "PasswordHash":
                    actual.PasswordHash.Should().Be(row[header]);
                    break;
                case "PhoneNumber":
                    actual.PhoneNumber.Should().Be(row[header]);
                    break;
                case "EmailConfirmed":
                    actual.EmailConfirmed.Should().Be(bool.Parse(row[header]));
                    break;
                case "PhoneNumberConfirmed":
                    actual.PhoneNumberConfirmed.Should().Be(bool.Parse(row[header]));
                    break;
                case "TwoFactorEnabled":
                    actual.TwoFactorEnabled.Should().Be(bool.Parse(row[header]));
                    break;
                case "LockoutEnabled":
                    actual.LockoutEnabled.Should().Be(bool.Parse(row[header]));
                    break;
                case "AccessFailedCount":
                    actual.AccessFailedCount.Should().Be(int.Parse(row[header]));
                    break;
                default:
                    break;
            }
        }
    }

    private static string BuildScenarioKey(string scenarioTitle)
    {
        var sanitized = new string(scenarioTitle.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        var prefix = string.IsNullOrWhiteSpace(sanitized) ? "scenario" : sanitized[..Math.Min(12, sanitized.Length)];
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scenarioTitle)))[..8].ToLowerInvariant();
        return $"{prefix}{hash}";
    }

    private string? MakeScenarioUniqueValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? value : $"{value}-{_scenarioKey}";

    private string MakeRequiredScenarioUniqueValue(string value) => MakeScenarioUniqueValue(value)!;

    private string MakeScenarioUniqueEmail(string value)
    {
        var separatorIndex = value.IndexOf('@');
        if (separatorIndex < 0)
        {
            return MakeScenarioUniqueValue(value)!;
        }

        var localPart = value[..separatorIndex];
        var domain = value[(separatorIndex + 1)..];
        return $"{localPart}+{_scenarioKey}@{domain}";
    }
}
