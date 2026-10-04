using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity;

public class ApplicationRoleClaim
    : IdentityRoleClaim<Guid>
{
    public string AuditSource { get; set; } = "role claim";
}
