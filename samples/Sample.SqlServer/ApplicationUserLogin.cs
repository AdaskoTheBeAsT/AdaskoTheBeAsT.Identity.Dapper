using System;
using Microsoft.AspNetCore.Identity;

namespace Sample.SqlServer;

public class ApplicationUserLogin
    : IdentityUserLogin<Guid>
{
}
