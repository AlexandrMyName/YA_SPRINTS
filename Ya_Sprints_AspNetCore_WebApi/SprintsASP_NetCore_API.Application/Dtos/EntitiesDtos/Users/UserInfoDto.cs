using SprintASP_NetCore_API.Domain.Entities;
using Sprints_Project_ASP_NetCore_API.Application.Dtos.EntitiesDtos; 


namespace SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;


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