using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class CreateUserRequest
    : IRequest<IdentityResult>
{
    public CreateUserRequest()
    {
        Roles = new List<string>();
    }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public ICollection<string> Roles { get; set; }
}
