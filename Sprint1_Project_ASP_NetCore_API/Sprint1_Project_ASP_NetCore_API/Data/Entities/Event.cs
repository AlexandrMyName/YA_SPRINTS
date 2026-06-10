using System.ComponentModel.DataAnnotations;


namespace Sprint1_Project_ASP_NetCore_API.Data.Entities
{

    public interface IEvent : IEntity
    {
        string Title { get; }
        string Description { get; }
        DateTime StartAt { get; }
        DateTime EndAt { get; }
    }


    public class Event : IEvent
    {
        [Key]
        public Guid Id { get; set; } 
        public required string Title { get; set; }
        public string? Description { get; set; }
        public required DateTime StartAt { get; set; }  
        public required DateTime EndAt { get; set; }
       
    }
}
