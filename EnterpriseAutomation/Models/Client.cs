using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomation.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public List<ServiceRequest> Requests { get; set; } = new();
    }
}
