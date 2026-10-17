using System.ComponentModel.DataAnnotations;


namespace Sprints.Users.Domain.Entities;


public interface IEntity
{

    [Key]

    Guid Id { get; set; } 
}
