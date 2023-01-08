using System;
using Microsoft.AspNetCore.Identity;

namespace Sample;

public class ApplicationRoleClaim
    : IdentityRoleClaim<Guid>
{
}
