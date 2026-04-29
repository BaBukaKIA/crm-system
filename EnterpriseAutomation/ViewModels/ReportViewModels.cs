namespace EnterpriseAutomation.ViewModels;

public record OrdersByPeriodItem(int OrderId, string ClientName, string Services, decimal Amount, DateTime DueDate, string PaymentStatus, string ExecutionStatus);
public record RequestsByStatusItem(string Status, int Count);
public record TopClientItem(string ClientName, int OrdersCount, decimal TotalAmount);

public class ReportsViewModel
{
    public DateTime From { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime To { get; set; } = DateTime.Today.AddMonths(1);
    public List<OrdersByPeriodItem> Orders { get; set; } = new();
    public List<RequestsByStatusItem> RequestsByStatuses { get; set; } = new();
    public List<TopClientItem> TopClients { get; set; } = new();
}
