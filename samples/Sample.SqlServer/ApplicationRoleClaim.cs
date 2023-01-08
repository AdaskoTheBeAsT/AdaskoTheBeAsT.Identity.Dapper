using System;
using Microsoft.AspNetCore.Identity;

namespace Sample.SqlServer;

public class ApplicationRoleClaim
    : IdentityRoleClaim<Guid>
{
}
