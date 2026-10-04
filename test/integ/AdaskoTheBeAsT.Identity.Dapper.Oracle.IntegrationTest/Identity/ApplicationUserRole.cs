using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity;

public class ApplicationUserRole
    : IdentityUserRole<Guid>
{
    public string AuditSource { get; set; } = "role link";
}
