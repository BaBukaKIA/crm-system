using System.Security.Claims;
using EnterpriseAutomation.Data;
using EnterpriseAutomation.Models;
using Microsoft.AspNetCore.Http;

namespace EnterpriseAutomation.Services.Auditing;

public sealed class AuditTrailWriter : IAuditTrailWriter
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditTrailWriter(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task WriteAsync(
        string action,
        string entityName,
        string? entityKey = null,
        string? details = null,
        string? actor = null,
        CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext;
        var resolvedActor = actor
            ?? context?.User?.Identity?.Name
            ?? "system";

        var record = new AuditLog
        {
            CreatedAt = DateTime.UtcNow,
            Actor = resolvedActor,
            ActorRole = context?.User?.FindFirstValue(ClaimTypes.Role),
            Action = action,
            EntityName = entityName,
            EntityKey = entityKey,
            Details = details,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            CorrelationId = context?.TraceIdentifier
        };

        _db.AuditLogs.Add(record);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
