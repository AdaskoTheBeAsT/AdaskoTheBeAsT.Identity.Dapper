using Microsoft.AspNetCore.Identity;

namespace Sample.SqlServer;

public class ApplicationUserClaim
    : IdentityUserClaim<Guid>
{
}
