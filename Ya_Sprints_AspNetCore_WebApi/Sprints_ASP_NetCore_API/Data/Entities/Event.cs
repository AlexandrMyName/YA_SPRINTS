using System.ComponentModel.DataAnnotations;


namespace Sprints_Project_ASP_NetCore_API.Data.Entities
{

    public interface IEvent : IEntity
    {
        string Title { get; }
        string Description { get; }
        DateTime StartAt { get; }
        DateTime EndAt { get; }
         
        int TotalSeats { get; set; }
        int AvailableSeats { get; set; }

        bool TryReserveSeats(int count = 1);
        void ReleaseSeats(int count = 1);
    }


    public class Event : IEvent
    {

        [Key]
        public Guid Id { get; set; } 
        public required string Title { get; set; }
        public string? Description { get; set; }
        public required DateTime StartAt { get; set; }  
        public required DateTime EndAt { get; set; }
         
        public int TotalSeats     { get; set; }  
        public int AvailableSeats { get; set; }


        public static Event Create(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
        {

            if (totalSeats <= 0)  throw new ValidationException("Общее количество мест должно быть больше 0");
            if (startAt >= endAt) throw new ValidationException("Дата начала не может быть позже или равна дате окончания");

            return new Event
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAt,
                TotalSeats = totalSeats,
                AvailableSeats = totalSeats
            };
        }


        public void ReleaseSeats(int count = 1){

            if((AvailableSeats + count) > TotalSeats) throw new InvalidOperationException("Значение превышает количество мест"); 
            AvailableSeats += count;
        }


        public bool TryReserveSeats(int count = 1){
            
            if (AvailableSeats >= count) { AvailableSeats -= count; return true; }
            else return false; 
        }

    }


    /// <summary>
    /// Исключение о нехватки доступных мест 
    /// </summary>
    public class NoAvailableSeatsException : Exception
    {
        public NoAvailableSeatsException(string errorMessage) : base(errorMessage)
        {

        }
    }
}
