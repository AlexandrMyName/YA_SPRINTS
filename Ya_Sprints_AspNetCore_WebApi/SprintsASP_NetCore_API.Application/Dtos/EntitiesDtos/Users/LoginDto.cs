using System.ComponentModel.DataAnnotations;


namespace SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;

 
public class LoginDto
{

    [Required] 
    public string Login { get; set; } = null!;
    [Required] 
    public string Password { get; set; } = null!;
}