using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomation.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите логин.")]
    [StringLength(80, ErrorMessage = "Логин не должен быть длиннее 80 символов.")]
    [Display(Name = "Логин")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль.")]
    [StringLength(120, MinimumLength = 4, ErrorMessage = "Пароль должен содержать от 4 до 120 символов.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
