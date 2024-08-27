using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.Identity;

public class ApplicationRole
    : IdentityRole<Guid>
{
    public override string? NormalizedName
    {
        get
        {
            return Name;
        }

#pragma warning disable S3237
        set
        {
            // noop
        }
#pragma warning restore S3237
    }
}
