using System;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using MediatR;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class GetUserByIdRequest
    : IRequest<UserModel?>
{
    public Guid? UserId { get; set; }
}
