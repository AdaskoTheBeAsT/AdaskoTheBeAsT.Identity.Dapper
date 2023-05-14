using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

public class UserModel
{
    public UserModel()
    {
        Roles = new List<string>();
    }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public ICollection<string> Roles { get; set; }
}
