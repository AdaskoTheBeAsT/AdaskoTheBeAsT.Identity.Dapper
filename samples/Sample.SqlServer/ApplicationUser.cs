using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Sample.SqlServer;

public class ApplicationUser
    : IdentityUser<Guid>
{
    [Column("IsActive")]
    public bool Active { get; set; }

    public override string? NormalizedUserName { get => UserName; set => UserName = value; }

    public override string? NormalizedEmail { get => Email; set => Email = value; }
}
