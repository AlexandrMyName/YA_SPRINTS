using SprintsASP_NetCore_API.Domain.Entities;


namespace SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;


public interface IRegisterUserDto 
{

    /// <summary>
    /// Логин пользователя 
    /// </summary>
    public string Login { get; }

    /// <summary>
    /// Пароль пользователя  
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// Роль пользователя 
    /// Для задания роли , 
    /// необходимо проверить, 
    /// что пользователь который регистрирует - является сам администратором
    /// Иначе - триггер не в регистрации 
    /// </summary>
    public UserRole Role { get; }  
}


public class RegisterUserDto : IRegisterUserDto
{ 

    /// <summary>
    /// Логин 
    /// </summary>
    public string Login { get; private set; }
    /// <summary>
    /// Пароль пользователя
    /// </summary>
    public string Password { get; private set; }

    /// <summary>
    /// Роль пользователя 
    /// </summary>
    public UserRole Role { get; } = UserRole.User;
     
}
