using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;

internal sealed class InMemoryUserStore :
    IUserPasswordStore<TestUser>,
    IUserEmailStore<TestUser>,
    IUserPhoneNumberStore<TestUser>,
    IUserSecurityStampStore<TestUser>,
    IUserLockoutStore<TestUser>,
    IUserTwoFactorStore<TestUser>,
    IUserAuthenticatorKeyStore<TestUser>,
    IUserTwoFactorRecoveryCodeStore<TestUser>,
    IUserAuthenticationTokenStore<TestUser>,
    IUserRoleStore<TestUser>,
    IUserClaimStore<TestUser>,
    IUserRoleClaimStore<TestUser>
{
    private const string InternalLoginProvider = "[AspNetUserStore]";
    private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
    private const string RecoveryCodesTokenName = "RecoveryCodes";

    private readonly Lock _sync = new();
    private readonly Dictionary<Guid, TestUser> _users = [];

    public IReadOnlyCollection<TestUser> Users
    {
        get
        {
            lock (_sync)
            {
                return _users.Values.ToArray();
            }
        }
    }

    public void Dispose()
    {
        // In-memory store holds no unmanaged resources.
    }

    public Task<string> GetUserIdAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString("D"));

    public Task<string?> GetUserNameAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(TestUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(TestUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(TestUser user, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _users[user.Id] = user;
        }

        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(TestUser user, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!_users.ContainsKey(user.Id))
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User does not exist." }));
            }

            _users[user.Id] = user;
        }

        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(TestUser user, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _users.Remove(user.Id);
        }

        return Task.FromResult(IdentityResult.Success);
    }

    public Task<TestUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var id))
        {
            return Task.FromResult<TestUser?>(null);
        }

        lock (_sync)
        {
            return Task.FromResult(_users.GetValueOrDefault(id));
        }
    }

    public Task<TestUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var match = _users.Values.FirstOrDefault(u =>
                string.Equals(u.NormalizedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(match);
        }
    }

    public Task SetPasswordHashAsync(TestUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    public Task SetEmailAsync(TestUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(TestUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<TestUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var match = _users.Values.FirstOrDefault(u =>
                string.Equals(u.NormalizedEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(match);
        }
    }

    public Task<string?> GetNormalizedEmailAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(TestUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task SetPhoneNumberAsync(TestUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task<string?> GetPhoneNumberAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PhoneNumber);

    public Task<bool> GetPhoneNumberConfirmedAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PhoneNumberConfirmed);

    public Task SetPhoneNumberConfirmedAsync(TestUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(TestUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.SecurityStamp);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(TestUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnabled);

    public Task SetLockoutEnabledAsync(TestUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task SetTwoFactorEnabledAsync(TestUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task<bool> GetTwoFactorEnabledAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.TwoFactorEnabled);

    public Task SetAuthenticatorKeyAsync(TestUser user, string key, CancellationToken cancellationToken) =>
        SetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, key, cancellationToken);

    public Task<string?> GetAuthenticatorKeyAsync(TestUser user, CancellationToken cancellationToken) =>
        GetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, cancellationToken);

    public Task ReplaceCodesAsync(TestUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken) =>
        SetTokenAsync(user, InternalLoginProvider, RecoveryCodesTokenName, string.Join(';', recoveryCodes), cancellationToken);

    public Task<bool> RedeemCodeAsync(TestUser user, string code, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!user.Tokens.TryGetValue((InternalLoginProvider, RecoveryCodesTokenName), out var raw) ||
                string.IsNullOrWhiteSpace(raw))
            {
                return Task.FromResult(false);
            }

            var split = raw.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (!split.Remove(code))
            {
                return Task.FromResult(false);
            }

            user.Tokens[(InternalLoginProvider, RecoveryCodesTokenName)] = string.Join(';', split);
            return Task.FromResult(true);
        }
    }

    public Task<int> CountCodesAsync(TestUser user, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!user.Tokens.TryGetValue((InternalLoginProvider, RecoveryCodesTokenName), out var raw) ||
                string.IsNullOrWhiteSpace(raw))
            {
                return Task.FromResult(0);
            }

            return Task.FromResult(raw.Split(';', StringSplitOptions.RemoveEmptyEntries).Length);
        }
    }

    public Task SetTokenAsync(
        TestUser user,
        string loginProvider,
        string name,
        string? value,
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (value == null)
            {
                user.Tokens.Remove((loginProvider, name));
            }
            else
            {
                user.Tokens[(loginProvider, name)] = value;
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveTokenAsync(TestUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            user.Tokens.Remove((loginProvider, name));
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetTokenAsync(TestUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult(user.Tokens.GetValueOrDefault((loginProvider, name)));
        }
    }

    public Task AddToRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken)
    {
        user.Roles.Add(roleName);
        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken)
    {
        user.Roles.Remove(roleName);
        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult<IList<string>>(user.Roles.ToList());

    public Task<bool> IsInRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken) =>
        Task.FromResult(user.Roles.Contains(roleName));

    public Task<IList<TestUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult<IList<TestUser>>(_users.Values.Where(u => u.Roles.Contains(roleName)).ToList());
        }
    }

    public Task<IList<Claim>> GetClaimsAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult<IList<Claim>>(user.Claims.ToList());

    public Task AddClaimsAsync(TestUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        user.Claims.AddRange(claims);
        return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(TestUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        user.Claims.RemoveAll(c =>
            string.Equals(c.Type, claim.Type, StringComparison.Ordinal) &&
            string.Equals(c.Value, claim.Value, StringComparison.Ordinal));
        user.Claims.Add(newClaim);
        return Task.CompletedTask;
    }

    public Task RemoveClaimsAsync(TestUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var target in claims)
        {
            user.Claims.RemoveAll(c =>
                string.Equals(c.Type, target.Type, StringComparison.Ordinal) &&
                string.Equals(c.Value, target.Value, StringComparison.Ordinal));
        }

        return Task.CompletedTask;
    }

    public Task<IList<TestUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult<IList<TestUser>>(
                _users.Values.Where(u => u.Claims.Any(c =>
                    string.Equals(c.Type, claim.Type, StringComparison.Ordinal) &&
                    string.Equals(c.Value, claim.Value, StringComparison.Ordinal))).ToList());
        }
    }

    public Task<IList<Claim>> GetRoleClaimsAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult<IList<Claim>>(Array.Empty<Claim>());

    public Task<IList<Claim>> GetUserAndRoleClaimsAsync(TestUser user, CancellationToken cancellationToken) =>
        Task.FromResult<IList<Claim>>(user.Claims.ToList());
}
