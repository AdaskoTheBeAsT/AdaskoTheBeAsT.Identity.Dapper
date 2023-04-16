using Microsoft.AspNetCore.Identity;

namespace Sample.SqlServer;

public class ApplicationUserToken
    : IdentityUserToken<Guid>
{
}
