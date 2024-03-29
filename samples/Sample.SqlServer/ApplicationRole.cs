using System;
using Microsoft.AspNetCore.Identity;

namespace Sample.SqlServer;

public class ApplicationRole
    : IdentityRole<Guid>
{
    public override string? NormalizedName { get => Name; set => Name = value; }
}
