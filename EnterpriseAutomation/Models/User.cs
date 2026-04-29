using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomation.Models;

public class User
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Login { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Role { get; set; } = UserRoles.Manager;

    public List<ServiceRequest> AssignedRequests { get; set; } = new();
}
