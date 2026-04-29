using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseAutomation.Models;

public class Order
{
    public int Id { get; set; }

    [Display(Name = "Заявка")]
    public int ServiceRequestId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }

    [Required, StringLength(1000), Display(Name = "Состав услуг")]
    public string Services { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,2)")]
    [Range(0, 999999999), Display(Name = "Сумма")]
    public decimal Amount { get; set; }

    [Display(Name = "Срок выполнения")]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);

    [Display(Name = "Статус оплаты")]
    public int PaymentStatusId { get; set; }
    public OrderPaymentStatus? PaymentStatus { get; set; }

    [Display(Name = "Статус исполнения")]
    public int ExecutionStatusId { get; set; }
    public OrderExecutionStatus? ExecutionStatus { get; set; }
}
