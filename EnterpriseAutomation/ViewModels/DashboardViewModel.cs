namespace EnterpriseAutomation.ViewModels;

// Kept as a compatibility wrapper for older code paths and the home dashboard view.
public class DashboardViewModel
{
    public bool IsAuthenticated { get; set; }

    public DashboardQuery Query { get; set; } = new();

    public DashboardSnapshot? Snapshot { get; set; }

    public int ClientsCount => Snapshot?.Metrics.ClientsCount ?? 0;

    public int RequestsCount => Snapshot?.Metrics.RequestsCount ?? 0;

    public int OrdersCount => Snapshot?.Metrics.OrdersCount ?? 0;

    public decimal OrdersAmount => Snapshot?.Metrics.RevenueAmount ?? 0;

    public int UnpaidOrdersCount => Snapshot?.Metrics.UnpaidOrdersCount ?? 0;
}
