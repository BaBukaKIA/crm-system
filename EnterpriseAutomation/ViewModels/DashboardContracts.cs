namespace EnterpriseAutomation.ViewModels;

public class DashboardQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Search { get; set; }
}

public sealed record DashboardSnapshot(
    DashboardMetrics Metrics,
    IReadOnlyList<DashboardChartSlice> RequestStatusSlices,
    IReadOnlyList<DashboardChartSlice> ExecutionStatusSlices,
    IReadOnlyList<DashboardTrendPoint> RevenueTrend,
    IReadOnlyList<DashboardBoardColumn> RequestBoard,
    IReadOnlyList<DashboardBoardColumn> OrderBoard,
    IReadOnlyList<DashboardActivityItem> Activity,
    IReadOnlyList<DashboardJobRunItem> JobRuns,
    IReadOnlyList<DashboardTopClientItem> TopClients,
    DateTime GeneratedAt)
{
    public IReadOnlyList<DashboardAttentionItem> AttentionItems { get; init; } = Array.Empty<DashboardAttentionItem>();
}

public sealed record DashboardMetrics(
    int ClientsCount,
    int RequestsCount,
    int OrdersCount,
    decimal RevenueAmount,
    int OpenRequestsCount,
    int ActiveOrdersCount,
    int UnpaidOrdersCount,
    int OverdueOrdersCount);

public sealed record DashboardChartSlice(int Id, string Label, int Count, string Color);

public sealed record DashboardTrendPoint(string Label, decimal Value);

public sealed record DashboardAttentionItem(
    string Title,
    string Details,
    string Tone,
    string Badge,
    string ActionLabel,
    string DetailUrl);

public sealed record DashboardBoardColumn(
    int Id,
    string Title,
    string Description,
    int TotalCount,
    string Color,
    IReadOnlyList<DashboardTaskCard> Cards);

public sealed record DashboardTaskCard(
    int Id,
    string Kind,
    string Title,
    string Subtitle,
    string Status,
    int StatusId,
    string DueLabel,
    bool IsOverdue,
    decimal? Amount,
    string Color,
    string DetailUrl);

public sealed record DashboardActivityItem(
    string Title,
    string Details,
    string Tone,
    DateTime CreatedAt);

public sealed record DashboardJobRunItem(
    string JobName,
    string Status,
    string Details,
    int RecordsProcessed,
    DateTime StartedAt,
    DateTime FinishedAt,
    string Tone);

public sealed record DashboardTopClientItem(
    string Name,
    int OrdersCount,
    decimal TotalAmount);

public sealed class DashboardTaskMoveRequest
{
    public string Kind { get; set; } = string.Empty;

    public int TaskId { get; set; }

    public int StatusId { get; set; }

    public DashboardQuery Query { get; set; } = new();
}
