using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Sample;

public class ApplicationUser
    : IdentityUser<Guid>
{
    [Column("IsActive")]
    public bool Active { get; set; }
}
