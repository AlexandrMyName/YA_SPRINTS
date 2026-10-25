using Sprints.Users.Domain.Entities;
using Sprints.Users.Application.Dtos.EntitiesDtos; 


namespace Sprints.Users.Application.Dtos.EntitiesDtos.Users;


public interface IUserInfoDto : IEntityDto
{
    string Login { get; set; }
    UserRole Role { get; set; }
    DateTime CreatedAt { get; set; }
}

public class UserInfoDto : IUserInfoDto
{
    public Guid Id { get; set; }
    public string Login { get; set; } = null!;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}