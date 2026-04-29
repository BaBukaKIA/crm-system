using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomation.Models;

public class OrderExecutionStatus
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string Name { get; set; } = string.Empty;
}
