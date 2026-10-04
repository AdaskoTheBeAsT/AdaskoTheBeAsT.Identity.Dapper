using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity;

public abstract class AuditedUser : IdentityUser<Guid>
{
    [Column("CreatedOn")]
    public DateTime? CreatedAt { get; set; }

    public string? DisplayLabel { get; set; } = "constructor default";
}
