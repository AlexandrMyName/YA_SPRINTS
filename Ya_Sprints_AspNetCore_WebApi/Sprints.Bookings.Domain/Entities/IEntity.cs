using System.ComponentModel.DataAnnotations;


namespace Sprints.Bookings.Domain.Entities;


public interface IEntity
{

    [Key]

    Guid Id { get; set; } 
}
