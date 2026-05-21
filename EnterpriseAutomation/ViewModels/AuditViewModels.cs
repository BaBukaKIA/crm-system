using EnterpriseAutomation.Models;

namespace EnterpriseAutomation.ViewModels;

public class AuditViewModel
{
    public string? Actor { get; set; }

    public string? EntityName { get; set; }

    public string? Action { get; set; }

    public IReadOnlyList<AuditLog> AuditLogs { get; set; } = Array.Empty<AuditLog>();

    public IReadOnlyList<AutomationRunLog> RunLogs { get; set; } = Array.Empty<AutomationRunLog>();
}
