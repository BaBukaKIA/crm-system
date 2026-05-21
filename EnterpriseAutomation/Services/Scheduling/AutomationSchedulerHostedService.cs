using EnterpriseAutomation.Data;
using EnterpriseAutomation.Models;
using EnterpriseAutomation.Services.Auditing;
using EnterpriseAutomation.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseAutomation.Services.Scheduling;

public sealed class AutomationSchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationSchedulerHostedService> _logger;
    private readonly SchedulerOptions _options;

    public AutomationSchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<SchedulerOptions> options,
        ILogger<AutomationSchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCycleAsync("startup", stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(15, _options.PollSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleAsync("timer", stoppingToken);
        }
    }

    private async Task RunCycleAsync(string trigger, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifier = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
            var audit = scope.ServiceProvider.GetRequiredService<IAuditTrailWriter>();

            var startedAt = DateTime.UtcNow;
            var reminderCutoff = DateTime.Today.AddDays(-Math.Max(1, _options.ReminderDays));

            var overdueOrders = await db.Orders
                .AsNoTracking()
                .Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client)
                .Where(x => x.ExecutionStatusId != 3 && x.DueDate.Date < DateTime.Today)
                .OrderBy(x => x.DueDate)
                .Take(5)
                .ToListAsync(cancellationToken);

            var staleRequests = await db.ServiceRequests
                .AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.RequestStatus)
                .Where(x => x.RequestStatusId != 3 && x.CreatedAt.Date <= reminderCutoff)
                .OrderBy(x => x.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken);

            var recordsProcessed = overdueOrders.Count + staleRequests.Count;
            var status = recordsProcessed == 0 ? "Healthy" : "Attention";
            var details = BuildDetails(overdueOrders, staleRequests);

            db.AutomationRunLogs.Add(new AutomationRunLog
            {
                JobName = "Operations check",
                Trigger = trigger,
                Status = status,
                RecordsProcessed = recordsProcessed,
                StartedAt = startedAt,
                FinishedAt = DateTime.UtcNow,
                Details = details
            });

            await db.SaveChangesAsync(cancellationToken);

            await audit.WriteAsync(
                "Scheduler run",
                "Operations check",
                entityKey: trigger,
                details: details,
                actor: "system",
                cancellationToken: cancellationToken);

            if (recordsProcessed > 0)
            {
                var notification = new NotificationMessage(
                    $"Operations check: {status}",
                    details,
                    RelatedEntity: "Scheduler",
                    RelatedEntityKey: trigger);

                var results = await notifier.SendAsync(notification, cancellationToken);
                _logger.LogInformation(
                    "Scheduler triggered {Count} alerts via {Channels}",
                    results.Count,
                    string.Join(", ", results.Select(x => x.Channel)));
            }
            else
            {
                _logger.LogInformation("Scheduler cycle completed with no overdue items.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduler cycle failed.");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AutomationRunLogs.Add(new AutomationRunLog
            {
                JobName = "Operations check",
                Trigger = trigger,
                Status = "Failed",
                StartedAt = DateTime.UtcNow,
                FinishedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message,
                Details = ex.ToString()
            });

            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private static string BuildDetails(
        IReadOnlyList<Order> overdueOrders,
        IReadOnlyList<ServiceRequest> staleRequests)
    {
        var parts = new List<string>();

        if (overdueOrders.Count > 0)
        {
            parts.Add($"Overdue orders: {overdueOrders.Count}");
        }

        if (staleRequests.Count > 0)
        {
            parts.Add($"Stale requests: {staleRequests.Count}");
        }

        if (parts.Count == 0)
        {
            parts.Add("No issues found");
        }

        return string.Join("; ", parts);
    }
}
