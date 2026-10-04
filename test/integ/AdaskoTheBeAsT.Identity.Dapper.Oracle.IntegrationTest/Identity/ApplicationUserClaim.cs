using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity;

public class ApplicationUserClaim
    : IdentityUserClaim<Guid>
{
    public string AuditSource { get; set; } = "user claim";
}
