using System.ComponentModel.DataAnnotations;

namespace Sprint1_Project_ASP_NetCore_API.Data.Entities
{
    public interface IEntity
    {

        [Key]

        Guid Id { get; set; }
    }
}
