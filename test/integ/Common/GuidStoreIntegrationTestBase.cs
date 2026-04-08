using System.Data;
using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Reqnroll;

namespace AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;

public abstract class GuidStoreIntegrationTestBase<
    TUser,
    TRole,
    TUserClaim,
    TUserRole,
    TUserLogin,
    TUserToken,
    TRoleClaim,
    TDbConnection>
    where TUser : IdentityUser<Guid>, new()
    where TRole : IdentityRole<Guid>, new()
    where TUserClaim : IdentityUserClaim<Guid>, new()
    where TUserRole : IdentityUserRole<Guid>, new()
    where TUserLogin : IdentityUserLogin<Guid>, new()
    where TUserToken : IdentityUserToken<Guid>, new()
    where TRoleClaim : IdentityRoleClaim<Guid>, new()
    where TDbConnection : IDbConnection
{
    private readonly Func<DapperRoleStoreBase<TRole, Guid, TRoleClaim, TDbConnection>> _roleStoreFactory;
    private readonly Func<DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>>
        _userOnlyStoreFactory;
    private readonly Func<DapperUserStoreBase<TUser, TRole, Guid, TUserClaim, TUserRole, TUserLogin, TUserToken, TDbConnection>>
        _userStoreFactory;

    protected GuidStoreIntegrationTestBase(
        Func<DapperRoleStoreBase<TRole, Guid, TRoleClaim, TDbConnection>> roleStoreFactory,
        Func<DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>> userOnlyStoreFactory,
        Func<DapperUserStoreBase<TUser, TRole, Guid, TUserClaim, TUserRole, TUserLogin, TUserToken, TDbConnection>> userStoreFactory)
    {
        _roleStoreFactory = roleStoreFactory ?? throw new ArgumentNullException(nameof(roleStoreFactory));
        _userOnlyStoreFactory = userOnlyStoreFactory ?? throw new ArgumentNullException(nameof(userOnlyStoreFactory));
        _userStoreFactory = userStoreFactory ?? throw new ArgumentNullException(nameof(userStoreFactory));
    }

    protected DapperRoleStoreBase<TRole, Guid, TRoleClaim, TDbConnection> CreateRoleStore() => _roleStoreFactory();

    protected DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>
        CreateUserOnlyStore() => _userOnlyStoreFactory();

    protected DapperUserStoreBase<TUser, TRole, Guid, TUserClaim, TUserRole, TUserLogin, TUserToken, TDbConnection>
        CreateUserStore() => _userStoreFactory();

    protected Task VerifyRoleStoreMethodAsync(string methodName) =>
        methodName switch
        {
            "Roles" => VerifyRoleStoreRolesAsync(),
            "Dispose" => VerifyRoleStoreDisposeAsync(),
            "CreateAsync" or
            "UpdateAsync" or
            "DeleteAsync" or
            "GetRoleIdAsync" or
            "GetRoleNameAsync" or
            "SetRoleNameAsync" or
            "ConvertIdFromString" or
            "ConvertIdToString" or
            "FindByIdAsync" or
            "FindByNameAsync" or
            "GetNormalizedRoleNameAsync" or
            "SetNormalizedRoleNameAsync" or
            "GetClaimsAsync" or
            "AddClaimAsync" or
            "RemoveClaimAsync" => VerifyRoleStoreMethodsAsync(CreateRoleStore),
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, message: null),
        };

    protected Task VerifyUserOnlyStoreMethodAsync(string methodName) =>
        VerifyCommonUserStoreMethodAsync(methodName, CreateUserOnlyStore);

    protected Task VerifyUserStoreMethodAsync(string methodName) =>
        methodName switch
        {
            "GetUsersInRoleAsync" or
            "AddToRoleAsync" or
            "RemoveFromRoleAsync" or
            "GetRolesAsync" or
            "IsInRoleAsync" or
            "GetRoleClaimsAsync" or
            "GetUserAndRoleClaimsAsync" => VerifyUserRoleMethodsAsync(CreateUserStore),
            _ => VerifyCommonUserStoreMethodAsync(methodName, CreateUserStore),
        };

    protected static TRole CreateRole(Table table, DataTableRow row)
    {
        var role = CreateRole();
        role.Id = GetGuidValue(table, row, "Id") ?? role.Id;
        role.Name = GetOptionalValue(table, row, "Name") ?? role.Name;
        role.ConcurrencyStamp = GetOptionalValue(table, row, "ConcurrencyStamp") ?? role.ConcurrencyStamp;
        return role;
    }

    protected static TUser CreateUser(Table table, DataTableRow row)
    {
        var user = CreateUser();
        user.Id = GetGuidValue(table, row, "Id") ?? user.Id;
        user.UserName = GetOptionalValue(table, row, "UserName") ?? user.UserName;
        user.Email = GetOptionalValue(table, row, "Email") ?? user.Email;
        user.EmailConfirmed = GetBoolValue(table, row, "EmailConfirmed") ?? user.EmailConfirmed;
        user.PasswordHash = GetOptionalValue(table, row, "PasswordHash") ?? user.PasswordHash;
        user.SecurityStamp = GetOptionalValue(table, row, "SecurityStamp") ?? user.SecurityStamp;
        user.ConcurrencyStamp = GetOptionalValue(table, row, "ConcurrencyStamp") ?? user.ConcurrencyStamp;
        user.PhoneNumber = GetOptionalValue(table, row, "PhoneNumber") ?? user.PhoneNumber;
        user.PhoneNumberConfirmed = GetBoolValue(table, row, "PhoneNumberConfirmed") ?? user.PhoneNumberConfirmed;
        user.TwoFactorEnabled = GetBoolValue(table, row, "TwoFactorEnabled") ?? user.TwoFactorEnabled;
        user.LockoutEnd = GetDateTimeOffsetValue(table, row, "LockoutEnd") ?? user.LockoutEnd;
        user.LockoutEnabled = GetBoolValue(table, row, "LockoutEnabled") ?? user.LockoutEnabled;
        user.AccessFailedCount = GetIntValue(table, row, "AccessFailedCount") ?? user.AccessFailedCount;
        return user;
    }

    protected static string GetRequiredValue(Table table, DataTableRow row, string columnName) =>
        GetOptionalValue(table, row, columnName) ??
        throw new InvalidOperationException($"Column '{columnName}' is required.");

    protected static string? GetOptionalValue(Table table, DataTableRow row, string columnName)
    {
        var header = table.Header.FirstOrDefault(
            h => string.Equals(h, columnName, StringComparison.OrdinalIgnoreCase));
        return header == null ? null : row[header];
    }

    protected static Guid? GetGuidValue(Table table, DataTableRow row, string columnName)
    {
        var value = GetOptionalValue(table, row, columnName);
        return value == null ? null : Guid.Parse(value);
    }

    protected static bool? GetBoolValue(Table table, DataTableRow row, string columnName)
    {
        var value = GetOptionalValue(table, row, columnName);
        return value == null ? null : bool.Parse(value);
    }

    protected static int? GetIntValue(Table table, DataTableRow row, string columnName)
    {
        var value = GetOptionalValue(table, row, columnName);
        return value == null ? null : int.Parse(value);
    }

    protected static DateTimeOffset? GetDateTimeOffsetValue(Table table, DataTableRow row, string columnName)
    {
        var value = GetOptionalValue(table, row, columnName);
        return value == null ? null : DateTimeOffset.Parse(value);
    }

    private Task VerifyCommonUserStoreMethodAsync(
        string methodName,
        Func<DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>> storeFactory) =>
        methodName switch
        {
            "Users" => VerifyUsersPropertyAsync(storeFactory),
            "Dispose" => VerifyUserStoreDisposeAsync(storeFactory),
            "GetClaimsAsync" or
            "AddClaimsAsync" or
            "ReplaceClaimAsync" or
            "RemoveClaimsAsync" or
            "GetUsersForClaimAsync" => VerifyUserClaimMethodsAsync(storeFactory),
            "AddLoginAsync" or
            "RemoveLoginAsync" or
            "GetLoginsAsync" or
            "FindByLoginAsync" => VerifyUserLoginMethodsAsync(storeFactory),
            "SetTokenAsync" or
            "RemoveTokenAsync" or
            "GetTokenAsync" or
            "SetAuthenticatorKeyAsync" or
            "GetAuthenticatorKeyAsync" or
            "CountCodesAsync" or
            "ReplaceCodesAsync" or
            "RedeemCodeAsync" => VerifyUserTokenAndRecoveryMethodsAsync(storeFactory),
            "GetUserIdAsync" or
            "GetUserNameAsync" or
            "SetUserNameAsync" or
            "GetNormalizedUserNameAsync" or
            "SetNormalizedUserNameAsync" or
            "CreateAsync" or
            "UpdateAsync" or
            "DeleteAsync" or
            "FindByIdAsync" or
            "ConvertIdFromString" or
            "ConvertIdToString" or
            "FindByNameAsync" or
            "SetPasswordHashAsync" or
            "GetPasswordHashAsync" or
            "HasPasswordAsync" or
            "GetEmailConfirmedAsync" or
            "SetEmailConfirmedAsync" or
            "SetEmailAsync" or
            "GetEmailAsync" or
            "GetNormalizedEmailAsync" or
            "SetNormalizedEmailAsync" or
            "FindByEmailAsync" or
            "GetLockoutEndDateAsync" or
            "SetLockoutEndDateAsync" or
            "IncrementAccessFailedCountAsync" or
            "ResetAccessFailedCountAsync" or
            "GetAccessFailedCountAsync" or
            "GetLockoutEnabledAsync" or
            "SetLockoutEnabledAsync" or
            "SetPhoneNumberAsync" or
            "GetPhoneNumberAsync" or
            "GetPhoneNumberConfirmedAsync" or
            "SetPhoneNumberConfirmedAsync" or
            "SetSecurityStampAsync" or
            "GetSecurityStampAsync" or
            "SetTwoFactorEnabledAsync" or
            "GetTwoFactorEnabledAsync" => VerifyCommonUserStoreMethodsAsync(storeFactory),
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, message: null),
        };

    private async Task VerifyRoleStoreRolesAsync()
    {
        using var store = CreateRoleStore();
        var role = CreateRole();

        AssertSucceeded(await store.CreateAsync(role, CancellationToken.None));

        store.Roles.Should().ContainSingle(x => x.Id == role.Id);
    }

    private async Task VerifyUsersPropertyAsync(
        Func<DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>> storeFactory)
    {
        using var roleStore = CreateRoleStore();
        using var userStore = CreateUserStore();
        var user = CreateUser();
        var role = CreateRole();

        AssertSucceeded(await userStore.CreateAsync(user, CancellationToken.None));
        AssertSucceeded(await roleStore.CreateAsync(role, CancellationToken.None));
        await userStore.AddToRoleAsync(user, role.Name!, CancellationToken.None);

        using var store = storeFactory();
        store.Users.Should().ContainSingle(x => x.Id == user.Id);
    }

    private async Task VerifyRoleStoreDisposeAsync()
    {
        var store = CreateRoleStore();
        store.Dispose();

        Func<Task> action = () => store.GetRoleIdAsync(CreateRole(), CancellationToken.None);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    private async Task VerifyUserStoreDisposeAsync(
        Func<DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>> storeFactory)
    {
        var store = storeFactory();
        store.Dispose();

        Func<Task> action = () => store.GetUserIdAsync(CreateUser(), CancellationToken.None);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    protected async Task VerifyRoleStoreMethodsAsync(
        Func<DapperRoleStoreBase<TRole, Guid, TRoleClaim, TDbConnection>> storeFactory)
    {
        using var store = storeFactory();
        var role = CreateRole();

        AssertSucceeded(await store.CreateAsync(role, CancellationToken.None));
        role.Id.Should().NotBeEmpty();

        var roleId = await store.GetRoleIdAsync(role, CancellationToken.None);
        roleId.Should().Be(role.Id.ToString());
        store.ConvertIdFromString(roleId).Should().Be(role.Id);
        store.ConvertIdToString(role.Id).Should().Be(roleId);

        (await store.GetRoleNameAsync(role, CancellationToken.None)).Should().Be(role.Name);
        (await store.GetNormalizedRoleNameAsync(role, CancellationToken.None)).Should().Be(role.Name);

        var updatedRoleName = $"role-updated-{Guid.NewGuid():N}";
        await store.SetRoleNameAsync(role, updatedRoleName, CancellationToken.None);
        await store.SetNormalizedRoleNameAsync(role, $"normalized-{Guid.NewGuid():N}", CancellationToken.None);
        role.ConcurrencyStamp = Guid.NewGuid().ToString("N");

        AssertSucceeded(await store.UpdateAsync(role, CancellationToken.None));

        var roleById = await store.FindByIdAsync(roleId, CancellationToken.None);
        roleById.Should().NotBeNull();
        roleById!.Id.Should().Be(role.Id);
        roleById.Name.Should().Be(updatedRoleName);

        var roleByName = await store.FindByNameAsync(updatedRoleName, CancellationToken.None);
        roleByName.Should().NotBeNull();
        roleByName!.Id.Should().Be(role.Id);

        var manageUsersClaim = new Claim("permission", "manage-users");
        var auditClaim = new Claim("permission", "audit-users");

        await store.AddClaimAsync(role, manageUsersClaim, CancellationToken.None);
        await store.AddClaimAsync(role, auditClaim, CancellationToken.None);

        ProjectClaims(await store.GetClaimsAsync(role, CancellationToken.None))
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    (manageUsersClaim.Type, manageUsersClaim.Value),
                    (auditClaim.Type, auditClaim.Value),
                });

        await store.RemoveClaimAsync(role, manageUsersClaim, CancellationToken.None);

        ProjectClaims(await store.GetClaimsAsync(role, CancellationToken.None))
            .Should()
            .BeEquivalentTo(new[] { (auditClaim.Type, auditClaim.Value) });

        await store.RemoveClaimAsync(role, auditClaim, CancellationToken.None);
        (await store.GetClaimsAsync(role, CancellationToken.None)).Should().BeEmpty();

        AssertSucceeded(await store.DeleteAsync(role, CancellationToken.None));
        (await store.FindByIdAsync(roleId, CancellationToken.None)).Should().BeNull();
    }

    protected async Task VerifyCommonUserStoreMethodsAsync(
        Func<DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>> storeFactory)
    {
        using var store = storeFactory();
        var user = CreateUser();

        AssertSucceeded(await store.CreateAsync(user, CancellationToken.None));

        var userId = await store.GetUserIdAsync(user, CancellationToken.None);
        userId.Should().Be(user.Id.ToString());
        store.ConvertIdFromString(userId).Should().Be(user.Id);
        store.ConvertIdToString(user.Id).Should().Be(userId);

        (await store.GetUserNameAsync(user, CancellationToken.None)).Should().Be(user.UserName);
        (await store.GetNormalizedUserNameAsync(user, CancellationToken.None)).Should().Be(user.UserName);

        var updatedUserName = $"user-updated-{Guid.NewGuid():N}";
        var updatedEmail = $"user-{Guid.NewGuid():N}@example.com";
        const string updatedPhoneNumber = "+15551234567";
        const string updatedPasswordHash = "updated-password-hash";
        const string updatedSecurityStamp = "updated-security-stamp";
        var updatedLockoutEnd = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

        await store.SetUserNameAsync(user, updatedUserName, CancellationToken.None);
        await store.SetNormalizedUserNameAsync(user, $"normalized-{Guid.NewGuid():N}", CancellationToken.None);
        await store.SetPasswordHashAsync(user, updatedPasswordHash, CancellationToken.None);
        await store.SetEmailAsync(user, updatedEmail, CancellationToken.None);
        await store.SetNormalizedEmailAsync(user, $"normalized-{Guid.NewGuid():N}@example.com", CancellationToken.None);
        await store.SetEmailConfirmedAsync(user, true, CancellationToken.None);
        await store.SetLockoutEndDateAsync(user, updatedLockoutEnd, CancellationToken.None);
        await store.SetLockoutEnabledAsync(user, true, CancellationToken.None);
        await store.SetPhoneNumberAsync(user, updatedPhoneNumber, CancellationToken.None);
        await store.SetPhoneNumberConfirmedAsync(user, true, CancellationToken.None);
        await store.SetSecurityStampAsync(user, updatedSecurityStamp, CancellationToken.None);
        await store.SetTwoFactorEnabledAsync(user, true, CancellationToken.None);

        (await store.GetPasswordHashAsync(user, CancellationToken.None)).Should().Be(updatedPasswordHash);
        (await store.HasPasswordAsync(user, CancellationToken.None)).Should().BeTrue();
        (await store.GetEmailAsync(user, CancellationToken.None)).Should().Be(updatedEmail);
        (await store.GetNormalizedEmailAsync(user, CancellationToken.None)).Should().Be(updatedEmail);
        (await store.GetEmailConfirmedAsync(user, CancellationToken.None)).Should().BeTrue();
        (await store.GetPhoneNumberAsync(user, CancellationToken.None)).Should().Be(updatedPhoneNumber);
        (await store.GetPhoneNumberConfirmedAsync(user, CancellationToken.None)).Should().BeTrue();
        (await store.GetLockoutEndDateAsync(user, CancellationToken.None)).Should().Be(updatedLockoutEnd);
        (await store.GetLockoutEnabledAsync(user, CancellationToken.None)).Should().BeTrue();
        (await store.GetSecurityStampAsync(user, CancellationToken.None)).Should().Be(updatedSecurityStamp);
        (await store.GetTwoFactorEnabledAsync(user, CancellationToken.None)).Should().BeTrue();

        AssertSucceeded(await store.UpdateAsync(user, CancellationToken.None));

        var userById = await store.FindByIdAsync(userId, CancellationToken.None);
        userById.Should().NotBeNull();
        userById!.Id.Should().Be(user.Id);

        var userByName = await store.FindByNameAsync(updatedUserName, CancellationToken.None);
        userByName.Should().NotBeNull();
        userByName!.Id.Should().Be(user.Id);

        var userByEmail = await store.FindByEmailAsync(updatedEmail, CancellationToken.None);
        userByEmail.Should().NotBeNull();
        userByEmail!.Id.Should().Be(user.Id);

        (await store.GetUserNameAsync(userById, CancellationToken.None)).Should().Be(updatedUserName);
        (await store.GetNormalizedUserNameAsync(userById, CancellationToken.None)).Should().Be(updatedUserName);
        (await store.GetPasswordHashAsync(userById, CancellationToken.None)).Should().Be(updatedPasswordHash);
        (await store.HasPasswordAsync(userById, CancellationToken.None)).Should().BeTrue();
        (await store.GetEmailAsync(userById, CancellationToken.None)).Should().Be(updatedEmail);
        (await store.GetNormalizedEmailAsync(userById, CancellationToken.None)).Should().Be(updatedEmail);
        (await store.GetEmailConfirmedAsync(userById, CancellationToken.None)).Should().BeTrue();
        (await store.GetPhoneNumberAsync(userById, CancellationToken.None)).Should().Be(updatedPhoneNumber);
        (await store.GetPhoneNumberConfirmedAsync(userById, CancellationToken.None)).Should().BeTrue();
        (await store.GetLockoutEndDateAsync(userById, CancellationToken.None)).Should().Be(updatedLockoutEnd);
        (await store.GetLockoutEnabledAsync(userById, CancellationToken.None)).Should().BeTrue();
        (await store.GetSecurityStampAsync(userById, CancellationToken.None)).Should().Be(updatedSecurityStamp);
        (await store.GetTwoFactorEnabledAsync(userById, CancellationToken.None)).Should().BeTrue();

        var accessFailedCount = await store.IncrementAccessFailedCountAsync(userById, CancellationToken.None);
        accessFailedCount.Should().Be(1);
        (await store.GetAccessFailedCountAsync(userById, CancellationToken.None)).Should().Be(1);

        var userAfterIncrement = await store.FindByIdAsync(userId, CancellationToken.None);
        userAfterIncrement.Should().NotBeNull();
        userAfterIncrement!.AccessFailedCount.Should().Be(1);

        await store.ResetAccessFailedCountAsync(userAfterIncrement, CancellationToken.None);
        (await store.GetAccessFailedCountAsync(userAfterIncrement, CancellationToken.None)).Should().Be(0);

        AssertSucceeded(await store.UpdateAsync(userAfterIncrement, CancellationToken.None));

        var userAfterReset = await store.FindByIdAsync(userId, CancellationToken.None);
        userAfterReset.Should().NotBeNull();
        userAfterReset!.AccessFailedCount.Should().Be(0);

        AssertSucceeded(await store.DeleteAsync(userAfterReset, CancellationToken.None));
        (await store.FindByIdAsync(userId, CancellationToken.None)).Should().BeNull();
    }

    protected async Task VerifyUserClaimMethodsAsync(
        Func<DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>> storeFactory)
    {
        using var store = storeFactory();
        var user = CreateUser();

        AssertSucceeded(await store.CreateAsync(user, CancellationToken.None));

        var readClaim = new Claim("permission", "read");
        var writeClaim = new Claim("permission", "write");
        var replaceClaim = new Claim("permission", "approve");

        await store.AddClaimsAsync(user, new[] { readClaim, writeClaim }, CancellationToken.None);

        ProjectClaims(await store.GetClaimsAsync(user, CancellationToken.None))
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    (readClaim.Type, readClaim.Value),
                    (writeClaim.Type, writeClaim.Value),
                });

        var usersForReadClaim = await store.GetUsersForClaimAsync(readClaim, CancellationToken.None);
        usersForReadClaim.Should().ContainSingle(x => x.Id == user.Id);

        await store.ReplaceClaimAsync(user, writeClaim, replaceClaim, CancellationToken.None);

        ProjectClaims(await store.GetClaimsAsync(user, CancellationToken.None))
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    (readClaim.Type, readClaim.Value),
                    (replaceClaim.Type, replaceClaim.Value),
                });

        await store.RemoveClaimsAsync(user, new[] { readClaim, replaceClaim }, CancellationToken.None);
        (await store.GetClaimsAsync(user, CancellationToken.None)).Should().BeEmpty();
    }

    protected async Task VerifyUserLoginMethodsAsync(
        Func<DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>> storeFactory)
    {
        using var store = storeFactory();
        var user = CreateUser();

        AssertSucceeded(await store.CreateAsync(user, CancellationToken.None));

        var login = new UserLoginInfo(
            "GitHub",
            $"github-{Guid.NewGuid():N}",
            "GitHub");

        await store.AddLoginAsync(user, login, CancellationToken.None);

        var logins = await store.GetLoginsAsync(user, CancellationToken.None);
        logins.Should().ContainSingle(
            x => x.LoginProvider == login.LoginProvider &&
                 x.ProviderKey == login.ProviderKey &&
                 x.ProviderDisplayName == login.ProviderDisplayName);

        var userByLogin = await store.FindByLoginAsync(login.LoginProvider, login.ProviderKey, CancellationToken.None);
        userByLogin.Should().NotBeNull();
        userByLogin!.Id.Should().Be(user.Id);

        await store.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey, CancellationToken.None);
        (await store.GetLoginsAsync(user, CancellationToken.None)).Should().BeEmpty();
        (await store.FindByLoginAsync(login.LoginProvider, login.ProviderKey, CancellationToken.None)).Should().BeNull();
    }

    protected async Task VerifyUserTokenAndRecoveryMethodsAsync(
        Func<DapperUserOnlyStoreBase<TUser, Guid, TUserClaim, TUserLogin, TUserToken, TDbConnection>> storeFactory)
    {
        using var store = storeFactory();
        var user = CreateUser();

        AssertSucceeded(await store.CreateAsync(user, CancellationToken.None));

        await store.SetTokenAsync(user, "api", "refresh-token", "refresh-value", CancellationToken.None);
        (await store.GetTokenAsync(user, "api", "refresh-token", CancellationToken.None)).Should().Be("refresh-value");

        await store.RemoveTokenAsync(user, "api", "refresh-token", CancellationToken.None);
        (await store.GetTokenAsync(user, "api", "refresh-token", CancellationToken.None)).Should().BeNull();

        await store.SetAuthenticatorKeyAsync(user, "auth-key", CancellationToken.None);
        (await store.GetAuthenticatorKeyAsync(user, CancellationToken.None)).Should().Be("auth-key");

        await store.ReplaceCodesAsync(user, new[] { "code-1", "code-2", "code-3" }, CancellationToken.None);
        (await store.CountCodesAsync(user, CancellationToken.None)).Should().Be(3);

        (await store.RedeemCodeAsync(user, "code-1", CancellationToken.None)).Should().BeTrue();
        (await store.CountCodesAsync(user, CancellationToken.None)).Should().Be(2);
        (await store.RedeemCodeAsync(user, "missing-code", CancellationToken.None)).Should().BeFalse();
    }

    protected async Task VerifyUserRoleMethodsAsync(
        Func<DapperUserStoreBase<TUser, TRole, Guid, TUserClaim, TUserRole, TUserLogin, TUserToken, TDbConnection>> storeFactory)
    {
        using var userStore = storeFactory();
        using var roleStore = CreateRoleStore();

        var user = CreateUser();
        var role = CreateRole();

        AssertSucceeded(await userStore.CreateAsync(user, CancellationToken.None));
        AssertSucceeded(await roleStore.CreateAsync(role, CancellationToken.None));

        var userClaim = new Claim("department", "engineering");
        var roleClaim = new Claim("permission", "manage-users");

        await userStore.AddClaimsAsync(user, new[] { userClaim }, CancellationToken.None);
        await roleStore.AddClaimAsync(role, roleClaim, CancellationToken.None);
        await userStore.AddToRoleAsync(user, role.Name!, CancellationToken.None);

        (await userStore.IsInRoleAsync(user, role.Name!, CancellationToken.None)).Should().BeTrue();
        (await userStore.GetRolesAsync(user, CancellationToken.None)).Should().BeEquivalentTo(new[] { role.Name! });

        var usersInRole = await userStore.GetUsersInRoleAsync(role.Name!, CancellationToken.None);
        usersInRole.Should().ContainSingle(x => x.Id == user.Id);

        ProjectClaims(await userStore.GetRoleClaimsAsync(user, CancellationToken.None))
            .Should()
            .BeEquivalentTo(new[] { (roleClaim.Type, roleClaim.Value) });

        ProjectClaims(await userStore.GetUserAndRoleClaimsAsync(user, CancellationToken.None))
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    (userClaim.Type, userClaim.Value),
                    (roleClaim.Type, roleClaim.Value),
                });

        await userStore.RemoveFromRoleAsync(user, role.Name!, CancellationToken.None);

        (await userStore.IsInRoleAsync(user, role.Name!, CancellationToken.None)).Should().BeFalse();
        (await userStore.GetRolesAsync(user, CancellationToken.None)).Should().BeEmpty();
        (await userStore.GetUsersInRoleAsync(role.Name!, CancellationToken.None)).Should().BeEmpty();
        (await userStore.GetRoleClaimsAsync(user, CancellationToken.None)).Should().BeEmpty();
    }

    private static IEnumerable<(string Type, string Value)> ProjectClaims(IEnumerable<Claim> claims) =>
        claims.Select(x => (x.Type, x.Value));

    private static TRole CreateRole() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = $"role-{Guid.NewGuid():N}",
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };

    private static TUser CreateUser() =>
        new()
        {
            Id = Guid.NewGuid(),
            UserName = $"user-{Guid.NewGuid():N}",
            Email = $"user-{Guid.NewGuid():N}@example.com",
            EmailConfirmed = false,
            PasswordHash = "initial-password-hash",
            SecurityStamp = "initial-security-stamp",
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            PhoneNumber = "+15550000000",
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            AccessFailedCount = 0,
        };

    private static void AssertSucceeded(IdentityResult result)
    {
        result.Errors.Should().BeEmpty();
        result.Succeeded.Should().BeTrue();
    }
}
