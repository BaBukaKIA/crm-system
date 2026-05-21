using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomation.Models;

public class AuditLog
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required, StringLength(120)]
    public string Actor { get; set; } = "system";

    [StringLength(60)]
    public string? ActorRole { get; set; }

    [Required, StringLength(60)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? EntityKey { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public string? Details { get; set; }

    [StringLength(80)]
    public string? IpAddress { get; set; }

    [StringLength(100)]
    public string? CorrelationId { get; set; }
}
