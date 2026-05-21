using EnterpriseAutomation.Data;
using EnterpriseAutomation.Models;
using EnterpriseAutomation.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAutomation.Services.Dashboard;

public sealed class DashboardSnapshotService : IDashboardSnapshotService
{
    private static readonly string[] Palette =
    [
        "#2563eb",
        "#0f766e",
        "#7c3aed",
        "#ea580c",
        "#16a34a",
        "#db2777",
        "#0284c7",
        "#4f46e5"
    ];

    private readonly AppDbContext _db;

    public DashboardSnapshotService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSnapshot> BuildAsync(DashboardQuery? query = null, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(query);
        var from = normalized.From!.Value.Date;
        var to = normalized.To!.Value.Date.AddDays(1).AddTicks(-1);
        var search = normalized.Search?.Trim();

        var requestQuery = _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.RequestStatus)
            .Include(x => x.Manager)
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .AsQueryable();

        var orderQuery = _db.Orders
            .AsNoTracking()
            .Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client)
            .Include(x => x.PaymentStatus)
            .Include(x => x.ExecutionStatus)
            .Where(x => x.DueDate >= from && x.DueDate <= to)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            requestQuery = requestQuery.Where(x =>
                x.Description.Contains(search!) ||
                x.Client!.Name.Contains(search!) ||
                (x.Manager != null && x.Manager.FullName.Contains(search!)));

            orderQuery = orderQuery.Where(x =>
                x.Services.Contains(search!) ||
                x.ServiceRequest!.Client!.Name.Contains(search!));
        }

        var requestStatuses = await _db.RequestStatuses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var executionStatuses = await _db.OrderExecutionStatuses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);

        var requests = await requestQuery.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        var orders = await orderQuery.OrderByDescending(x => x.DueDate).ToListAsync(cancellationToken);

        var requestStatusSlices = requestStatuses
            .Select((status, index) => new DashboardChartSlice(
                status.Id,
                status.Name,
                requests.Count(x => x.RequestStatusId == status.Id),
                Palette[index % Palette.Length]))
            .ToList();

        var executionStatusSlices = executionStatuses
            .Select((status, index) => new DashboardChartSlice(
                status.Id,
                status.Name,
                orders.Count(x => x.ExecutionStatusId == status.Id),
                Palette[(index + 3) % Palette.Length]))
            .ToList();

        var revenueTrend = BuildRevenueTrend(orders, normalized.From!.Value, normalized.To!.Value).ToList();

        var requestBoard = BuildRequestBoard(requestStatuses, requests).ToList();
        var orderBoard = BuildOrderBoard(executionStatuses, orders).ToList();

        var activity = await BuildActivityAsync(cancellationToken);
        var jobRuns = await BuildJobRunsAsync(cancellationToken);
        var topClients = BuildTopClients(orders);

        var metrics = new DashboardMetrics(
            ClientsCount: await _db.Clients.AsNoTracking().CountAsync(cancellationToken),
            RequestsCount: requests.Count,
            OrdersCount: orders.Count,
            RevenueAmount: orders.Sum(x => x.Amount),
            OpenRequestsCount: requests.Count(x => x.RequestStatusId != 3),
            ActiveOrdersCount: orders.Count(x => x.ExecutionStatusId != 3),
            UnpaidOrdersCount: orders.Count(x => x.PaymentStatusId != 3),
            OverdueOrdersCount: orders.Count(x => x.ExecutionStatusId != 3 && x.DueDate.Date < DateTime.Today));

        return new DashboardSnapshot(
            metrics,
            requestStatusSlices,
            executionStatusSlices,
            revenueTrend,
            requestBoard,
            orderBoard,
            activity,
            jobRuns,
            topClients,
            DateTime.UtcNow)
        {
            AttentionItems = DashboardAttentionBuilder.Build(requests, orders)
        };
    }

    public async Task<DashboardSnapshot> MoveTaskAsync(DashboardTaskMoveRequest request, CancellationToken cancellationToken = default)
    {
        switch (request.Kind.Trim().ToLowerInvariant())
        {
            case "request":
                var serviceRequest = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == request.TaskId, cancellationToken);
                if (serviceRequest == null)
                {
                    throw new KeyNotFoundException("Request not found.");
                }

                serviceRequest.RequestStatusId = request.StatusId;
                break;

            case "order":
                var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == request.TaskId, cancellationToken);
                if (order == null)
                {
                    throw new KeyNotFoundException("Order not found.");
                }

                order.ExecutionStatusId = request.StatusId;
                break;

            default:
                throw new InvalidOperationException("Unsupported task kind.");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await BuildAsync(request.Query, cancellationToken);
    }

    private static DashboardQuery Normalize(DashboardQuery? query)
    {
        return new DashboardQuery
        {
            From = (query?.From ?? DateTime.Today.AddDays(-14)).Date,
            To = (query?.To ?? DateTime.Today).Date,
            Search = query?.Search
        };
    }

    private static IEnumerable<DashboardTrendPoint> BuildRevenueTrend(
        IReadOnlyList<Order> orders,
        DateTime from,
        DateTime to)
    {
        for (var date = from.Date; date.Date <= to.Date; date = date.AddDays(1))
        {
            var total = orders.Where(x => x.DueDate.Date == date.Date).Sum(x => x.Amount);
            yield return new DashboardTrendPoint(date.ToString("dd.MM"), total);
        }
    }

    private static IEnumerable<DashboardBoardColumn> BuildRequestBoard(
        IReadOnlyList<RequestStatus> statuses,
        IReadOnlyList<ServiceRequest> requests)
    {
        foreach (var (status, index) in statuses.Select((item, index) => (item, index)))
        {
            var cards = requests
                .Where(x => x.RequestStatusId == status.Id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new DashboardTaskCard(
                    x.Id,
                    "request",
                    x.Description.Truncate(72),
                    $"{x.Client?.Name ?? "Клиент"} • {x.Manager?.FullName ?? "без исполнителя"}",
                    x.RequestStatus?.Name ?? status.Name,
                    x.RequestStatusId,
                    x.CreatedAt.ToString("dd.MM.yyyy"),
                    x.CreatedAt.Date < DateTime.Today.AddDays(-7),
                    null,
                    Palette[index % Palette.Length],
                    $"/Requests/Edit/{x.Id}"))
                .ToList();

            yield return new DashboardBoardColumn(
                status.Id,
                status.Name,
                $"{cards.Count} заявок",
                cards.Count,
                Palette[index % Palette.Length],
                cards);
        }
    }

    private static IEnumerable<DashboardBoardColumn> BuildOrderBoard(
        IReadOnlyList<OrderExecutionStatus> statuses,
        IReadOnlyList<Order> orders)
    {
        foreach (var (status, index) in statuses.Select((item, index) => (item, index)))
        {
            var cards = orders
                .Where(x => x.ExecutionStatusId == status.Id)
                .OrderBy(x => x.DueDate)
                .Select(x => new DashboardTaskCard(
                    x.Id,
                    "order",
                    x.ServiceRequest?.Client?.Name ?? $"Заказ #{x.Id}",
                    x.Services.Truncate(72),
                    x.ExecutionStatus?.Name ?? status.Name,
                    x.ExecutionStatusId,
                    x.DueDate.ToString("dd.MM.yyyy"),
                    x.DueDate.Date < DateTime.Today && x.ExecutionStatusId != 3,
                    x.Amount,
                    Palette[(index + 3) % Palette.Length],
                    $"/Orders/Edit/{x.Id}"))
                .ToList();

            yield return new DashboardBoardColumn(
                status.Id,
                status.Name,
                $"{cards.Count} заказов",
                cards.Count,
                Palette[(index + 3) % Palette.Length],
                cards);
        }
    }

    private async Task<IReadOnlyList<DashboardActivityItem>> BuildActivityAsync(CancellationToken cancellationToken)
    {
        var auditLogs = await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        var systemLogs = await _db.AutomationRunLogs
            .AsNoTracking()
            .OrderByDescending(x => x.StartedAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        var items = auditLogs.Select(log => new DashboardActivityItem(
                $"{log.Action} • {log.EntityName}",
                log.Details ?? BuildAuditDetails(log),
                MapTone(log.Action),
                log.CreatedAt))
            .Concat(systemLogs.Select(log => new DashboardActivityItem(
                log.JobName,
                log.Details ?? $"{log.Trigger}: {log.Status}",
                MapTone(log.Status),
                log.StartedAt)))
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .ToList();

        return items;
    }

    private async Task<IReadOnlyList<DashboardJobRunItem>> BuildJobRunsAsync(CancellationToken cancellationToken)
    {
        var runs = await _db.AutomationRunLogs
            .AsNoTracking()
            .OrderByDescending(x => x.StartedAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        return runs
            .Select(run => new DashboardJobRunItem(
                run.JobName,
                run.Status,
                run.Details ?? run.ErrorMessage ?? run.Trigger,
                run.RecordsProcessed,
                run.StartedAt,
                run.FinishedAt,
                MapTone(run.Status)))
            .ToList();
    }

    private static IReadOnlyList<DashboardTopClientItem> BuildTopClients(IReadOnlyList<Order> orders)
    {
        return orders
            .GroupBy(x => x.ServiceRequest?.Client?.Name ?? "Неизвестный клиент")
            .Select(group => new DashboardTopClientItem(
                group.Key,
                group.Count(),
                group.Sum(x => x.Amount)))
            .OrderByDescending(x => x.TotalAmount)
            .ThenBy(x => x.Name)
            .Take(5)
            .ToList();
    }

    private static string BuildAuditDetails(AuditLog log)
    {
        if (!string.IsNullOrWhiteSpace(log.EntityKey))
        {
            return $"# {log.EntityKey}";
        }

        return log.Actor;
    }

    private static string MapTone(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            var text when text.Contains("fail") || text.Contains("error") || text.Contains("удал") => "danger",
            var text when text.Contains("warn") || text.Contains("attention") || text.Contains("pending") => "warning",
            var text when text.Contains("done") || text.Contains("success") || text.Contains("created") || text.Contains("updated") || text.Contains("signed") => "success",
            _ => "info"
        };
    }
}

internal static class DashboardTextExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - 1)].TrimEnd() + "…";
    }
}
