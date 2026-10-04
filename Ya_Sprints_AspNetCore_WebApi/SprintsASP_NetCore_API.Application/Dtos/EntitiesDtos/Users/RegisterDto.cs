using System.ComponentModel.DataAnnotations;


namespace SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;
  

public class RegisterDto
{
    [Required(ErrorMessage = "Логин обязателен")]
    [StringLength(50, MinimumLength = 3)]
    public string Login { get; set; } = null!;

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = null!;
}
