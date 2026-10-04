using SprintASP_NetCore_API.Domain.Entities; 


namespace SprintsASP_NetCore_API.Domain.Entities;


public interface IUser : IEntity
{
     
    public string Login { get;   } 
    public string PasswordHash { get; } 
    public UserRole Role { get; }
}


public class User : IUser
{

    public Guid Id { get; set; }
    public string Login { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }


    private User() { }


    public static User Create(Guid id, string login, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException("Login is empty", nameof(login));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is empty", nameof(passwordHash));

        return new User
        {
            Id = id,
            Login = login,
            PasswordHash = passwordHash,
            Role = role
        };
    }
}


public enum UserRole
{
    User = 0,
    Admin = 1
}
