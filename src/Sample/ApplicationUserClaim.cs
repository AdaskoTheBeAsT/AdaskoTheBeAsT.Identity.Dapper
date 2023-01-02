using System;
using Microsoft.AspNetCore.Identity;

namespace Sample;

public class ApplicationUserClaim
    : IdentityUserClaim<Guid>
{
}
