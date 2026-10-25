using System.ComponentModel.DataAnnotations;


namespace Sprints.Users.Application.Dtos.EntitiesDtos.Users;

 
public class LoginDto
{

    [Required] 
    public string Login { get; set; } = null!;
    [Required] 
    public string Password { get; set; } = null!;
}