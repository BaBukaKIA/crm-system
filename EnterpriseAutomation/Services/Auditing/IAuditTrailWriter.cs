namespace EnterpriseAutomation.Services.Auditing;

public interface IAuditTrailWriter
{
    Task WriteAsync(
        string action,
        string entityName,
        string? entityKey = null,
        string? details = null,
        string? actor = null,
        CancellationToken cancellationToken = default);
}
