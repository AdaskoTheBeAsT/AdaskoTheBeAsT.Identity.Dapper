using AdaskoTheBeAsT.Identity.Dapper.Attributes;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.Identity;

[InsertOwnId]
public class ApplicationUser
    : IdentityUser<Guid>
{
    public override string? NormalizedUserName
    {
        get
        {
            return UserName;
        }

#pragma warning disable S3237
        set
        {
            // noop
        }
#pragma warning restore S3237
    }

    public override string? NormalizedEmail
    {
        get
        {
            return Email;
        }

#pragma warning disable S3237
        set
        {
            // noop
        }
#pragma warning restore S3237
    }
}
