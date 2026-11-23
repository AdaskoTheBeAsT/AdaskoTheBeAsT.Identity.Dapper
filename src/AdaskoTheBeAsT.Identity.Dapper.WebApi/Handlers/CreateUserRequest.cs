using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class CreateUserRequest
{
    public CreateUserRequest()
    {
        Roles = new List<string>();
    }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public ICollection<string> Roles { get; set; }
}
