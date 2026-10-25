using System.ComponentModel.DataAnnotations;


namespace Sprints.Events.Domain.Entities;


public interface IEntity
{

    [Key]

    Guid Id { get; set; } 
}
