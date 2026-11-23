using Microsoft.AspNetCore.Identity;

namespace JwtSpa.Api;

public sealed class ApplicationUserToken : IdentityUserToken<Guid>
{
}
