 

namespace SprintASP_NetCore_API.Domain.Entities;


public interface IUser : IEntity
{
     
    string Login { get;  } 
    string PasswordHash { get;   } 
    UserRole Role { get;   }
    DateTime CreatedAt { get; }
    void PromoteToAdmin();
    void DemoteToUser();
    void ChangeLogin(string newLogin);
}


public class User : IUser
{
    public Guid Id { get; set; }
    public string Login { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

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
            Login = login.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void ChangeLogin(string newLogin)
    {
        if (string.IsNullOrWhiteSpace(newLogin))
            throw new ArgumentException("Login is empty", nameof(newLogin));
        Login = newLogin.Trim();
    }

    public void PromoteToAdmin() => Role = UserRole.Admin;
    public void DemoteToUser() => Role = UserRole.User;
}


public enum UserRole
{
    User = 0,
    Admin = 1
}
