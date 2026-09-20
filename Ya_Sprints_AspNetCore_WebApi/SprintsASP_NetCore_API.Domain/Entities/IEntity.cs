using System.ComponentModel.DataAnnotations;


namespace SprintASP_NetCore_API.Domain.Entities;


public interface IEntity
{

    [Key]

    Guid Id { get; set; } 
}
