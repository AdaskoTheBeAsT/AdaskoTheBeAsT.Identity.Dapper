using System;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;

public class ApplicationUser
    : IdentityUser<Guid>
{
    public override string? NormalizedUserName { get => UserName; set => UserName = value; }

    public override string? NormalizedEmail { get => Email; set => Email = value; }
}
