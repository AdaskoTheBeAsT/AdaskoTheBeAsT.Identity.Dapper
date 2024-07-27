using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class CreateRoleRequest
    : IRequest<IdentityResult>
{
    public string? Name { get; set; }
}
