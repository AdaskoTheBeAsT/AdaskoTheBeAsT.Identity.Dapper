using System;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class GetUserByIdRequest
{
    public Guid? UserId { get; set; }
}
