using System;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class DeleteUserRequest
{
    public Guid? UserId { get; set; }
}
