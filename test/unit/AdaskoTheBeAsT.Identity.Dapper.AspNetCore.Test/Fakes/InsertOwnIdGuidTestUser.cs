using AdaskoTheBeAsT.Identity.Dapper.Attributes;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;

[InsertOwnId]
public sealed class InsertOwnIdGuidTestUser : IdentityUser<Guid>
{
}
