using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomation.Models;

public class ServiceRequest
{
    public int Id { get; set; }

    [Display(Name = "Клиент")]
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    [Display(Name = "Дата создания")]
    public DateTime CreatedAt { get; set; } = DateTime.Today;

    [Required, StringLength(1000), Display(Name = "Описание")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Статус")]
    public int RequestStatusId { get; set; }
    public RequestStatus? RequestStatus { get; set; }

    [Display(Name = "Ответственный менеджер")]
    public int ManagerId { get; set; }
    public User? Manager { get; set; }

    public Order? Order { get; set; }
}
