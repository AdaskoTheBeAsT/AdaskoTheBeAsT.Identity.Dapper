using System;
using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class UpdateUserRequest
    : IRequest<IdentityResult>
{
    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public ICollection<string>? Roles { get; set; }
}
