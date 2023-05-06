using System;
using System.ComponentModel.DataAnnotations;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

public class RefreshToken
{
    public string? RefreshTokenId { get; set; }

    [Required]
    [MaxLength(50)]
    public string? Subject { get; set; }

    [Required]
    [MaxLength(32)]
    public string? AudienceId { get; set; }

    public DateTime IssuedUtc { get; set; }

    public DateTime ExpiresUtc { get; set; }

    [Required]
    public string? ProtectedTicket { get; set; }
}
