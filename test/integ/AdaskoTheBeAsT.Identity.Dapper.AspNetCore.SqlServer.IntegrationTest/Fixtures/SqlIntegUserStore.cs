using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest.Fixtures;

internal sealed class SqlIntegUserStore :
    IUserPasswordStore<SqlIntegUser>,
    IUserSecurityStampStore<SqlIntegUser>,
    IUserClaimStore<SqlIntegUser>
{
    private readonly ConcurrentDictionary<Guid, SqlIntegUser> _users = new();
    private readonly ConcurrentDictionary<Guid, List<Claim>> _claims = new();

    public void Dispose()
    {
    }

    public Task<IdentityResult> CreateAsync(SqlIntegUser user, CancellationToken cancellationToken)
    {
        _users[user.Id] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(SqlIntegUser user, CancellationToken cancellationToken)
    {
        _users.AddOrUpdate(user.Id, user, (_, _) => user);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(SqlIntegUser user, CancellationToken cancellationToken)
    {
        _users.TryRemove(user.Id, out _);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<SqlIntegUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(Guid.TryParse(userId, out var id) && _users.TryGetValue(id, out var u) ? u : null);

    public Task<SqlIntegUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => string.Equals(u.NormalizedUserName, normalizedUserName, StringComparison.Ordinal)));

    public Task<string> GetUserIdAsync(SqlIntegUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString("D"));

    public Task<string?> GetUserNameAsync(SqlIntegUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(SqlIntegUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(SqlIntegUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(SqlIntegUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(SqlIntegUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(SqlIntegUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(SqlIntegUser user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(user.PasswordHash));

    public Task SetSecurityStampAsync(SqlIntegUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(SqlIntegUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.SecurityStamp);

    public Task<IList<Claim>> GetClaimsAsync(SqlIntegUser user, CancellationToken cancellationToken)
    {
        var list = _claims.TryGetValue(user.Id, out var existing)
            ? existing.ToList()
            : new List<Claim>();
        return Task.FromResult<IList<Claim>>(list);
    }

    public Task AddClaimsAsync(SqlIntegUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        var list = _claims.GetOrAdd(user.Id, _ => []);
        lock (list)
        {
            list.AddRange(claims);
        }

        return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(SqlIntegUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        var list = _claims.GetOrAdd(user.Id, _ => []);
        lock (list)
        {
            var idx = list.FindIndex(c =>
                string.Equals(c.Type, claim.Type, StringComparison.Ordinal) &&
                string.Equals(c.Value, claim.Value, StringComparison.Ordinal));
            if (idx >= 0)
            {
                list[idx] = newClaim;
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveClaimsAsync(SqlIntegUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        if (_claims.TryGetValue(user.Id, out var list))
        {
            lock (list)
            {
                foreach (var claim in claims)
                {
                    list.RemoveAll(c =>
                        string.Equals(c.Type, claim.Type, StringComparison.Ordinal) &&
                        string.Equals(c.Value, claim.Value, StringComparison.Ordinal));
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<IList<SqlIntegUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken) =>
        Task.FromResult<IList<SqlIntegUser>>(Array.Empty<SqlIntegUser>());
}
