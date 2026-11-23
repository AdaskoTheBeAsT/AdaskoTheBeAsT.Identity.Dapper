using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest.Fixtures;

public sealed class SqlIntegUser : IdentityUser<Guid>
{
    public SqlIntegUser()
    {
        Id = Guid.NewGuid();
        SecurityStamp = Guid.NewGuid().ToString("N");
    }
}
