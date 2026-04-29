namespace EnterpriseAutomation.ViewModels;

public class DashboardViewModel
{
    public int ClientsCount { get; set; }
    public int RequestsCount { get; set; }
    public int OrdersCount { get; set; }
    public decimal OrdersAmount { get; set; }
    public int UnpaidOrdersCount { get; set; }
}
