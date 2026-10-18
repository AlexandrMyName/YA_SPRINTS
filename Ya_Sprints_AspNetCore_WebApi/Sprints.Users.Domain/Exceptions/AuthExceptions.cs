

namespace Sprints.Users.Domain.Exceptions;

public class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException(string login)
        : base($"Пользователь с логином '{login}' уже существует") { }
}

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Неверный логин или пароль") { }
}

public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException() : base("Недействительный refresh token") { }
}