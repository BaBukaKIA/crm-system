using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomation.ViewModels;

public class LoginViewModel
{
    [Required, Display(Name = "Логин")]
    public string Login { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;
}
