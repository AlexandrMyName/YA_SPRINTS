using System.ComponentModel.DataAnnotations;

namespace Sprints_Project_ASP_NetCore_API.Data.Entities
{
    public interface IEntity
    {

        [Key]

        Guid Id { get; set; }
    }
}
