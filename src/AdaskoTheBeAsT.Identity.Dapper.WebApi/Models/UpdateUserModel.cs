using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

public class UpdateUserModel
{
    public string? UserName { get; set; }

    public ICollection<string>? Roles { get; set; }
}
