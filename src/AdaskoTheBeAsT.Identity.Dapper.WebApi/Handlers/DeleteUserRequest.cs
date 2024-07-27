using System;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class DeleteUserRequest
    : IRequest<IdentityResult>
{
    public Guid? UserId { get; set; }
}
