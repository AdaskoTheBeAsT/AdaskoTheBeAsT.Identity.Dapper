using System;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;

public class ApplicationRole
    : IdentityRole<Guid>
{
    public override string? NormalizedName { get => Name; set => Name = value; }
}
