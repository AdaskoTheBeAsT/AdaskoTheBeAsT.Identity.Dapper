using Microsoft.AspNetCore.Identity;

namespace Sample.SqlServer2;

public class ApplicationUserToken
    : IdentityUserToken<Guid>
{
}
