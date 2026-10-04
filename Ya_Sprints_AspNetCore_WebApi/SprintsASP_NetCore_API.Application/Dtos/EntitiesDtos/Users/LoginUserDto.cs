 

namespace SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;


public interface ILoginUserDto 
{

    /// <summary>
    /// Логин пользователя 
    /// </summary>
    public string Login { get; set; }

    /// <summary>
    /// Пароль пользователя  
    /// </summary>
    public string Password { get; set; } 
}


public class LoginUserDto : ILoginUserDto
{

    public string Login { get; set; }
    public string Password { get; set; } 
}
