namespace EnterpriseAutomation.ViewModels;

public class ClientFilter
{
    public string? Search { get; set; }
    public string Sort { get; set; } = "name";
}

public class RequestFilter
{
    public string? Search { get; set; }
    public int? StatusId { get; set; }
    public int? ManagerId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string Sort { get; set; } = "date_desc";
}

public class OrderFilter
{
    public string? Search { get; set; }
    public int? PaymentStatusId { get; set; }
    public int? ExecutionStatusId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string Sort { get; set; } = "due";
}
