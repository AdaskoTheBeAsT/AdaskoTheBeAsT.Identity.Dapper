using System;
using System.ComponentModel.DataAnnotations;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

public class RefreshToken
{
    [Required]
    [MaxLength(50)]
    public string? Subject { get; set; }

    public string? SecurityStamp { get; set; }

    [Required]
    [MaxLength(32)]
    public string? AudienceId { get; set; }

    public DateTime ExpiresUtc { get; set; }
}
