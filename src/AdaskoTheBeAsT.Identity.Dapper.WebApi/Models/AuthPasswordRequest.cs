namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

public class AuthPasswordRequest
    : AuthRequestBase
{
    public string? Username { get; set; }

    public string? Password { get; set; }
}
