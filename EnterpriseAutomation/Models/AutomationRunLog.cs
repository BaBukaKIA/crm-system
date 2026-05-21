using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomation.Models;

public class AutomationRunLog
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string JobName { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Trigger { get; set; } = "Scheduler";

    [Required, StringLength(40)]
    public string Status { get; set; } = "Completed";

    public int RecordsProcessed { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime FinishedAt { get; set; }

    [StringLength(1000)]
    public string? Details { get; set; }

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
}
