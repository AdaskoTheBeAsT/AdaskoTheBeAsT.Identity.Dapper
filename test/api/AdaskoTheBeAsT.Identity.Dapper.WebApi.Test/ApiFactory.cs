using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Test;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("TokenServiceOptions:SigningKey", SigningKey);
        builder.UseEnvironment("Testing");
    }
}
