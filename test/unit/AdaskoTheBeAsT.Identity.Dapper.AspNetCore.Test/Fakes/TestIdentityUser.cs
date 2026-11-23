using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;

public sealed class TestIdentityUser<TKey> : IdentityUser<TKey>
    where TKey : IEquatable<TKey>
{
    public TestIdentityUser(TKey id, string userName, string? email = null)
    {
        Id = id;
        UserName = userName;
        NormalizedUserName = userName.ToUpperInvariant();
        Email = email;
        NormalizedEmail = email?.ToUpperInvariant();
        SecurityStamp = Guid.NewGuid().ToString("N");
    }
}
