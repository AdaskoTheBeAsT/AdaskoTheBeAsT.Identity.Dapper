using AdaskoTheBeAsT.Identity.Dapper.Attributes;
using Microsoft.AspNetCore.Identity;

namespace JwtSpa.Api;

[InsertOwnId]
public sealed class ApplicationUser : IdentityUser<Guid>
{
}
