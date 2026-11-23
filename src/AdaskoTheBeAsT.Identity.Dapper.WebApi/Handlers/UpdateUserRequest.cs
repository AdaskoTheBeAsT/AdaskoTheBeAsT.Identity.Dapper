using System;
using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class UpdateUserRequest
{
    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public ICollection<string>? Roles { get; set; }
}
