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
         
        public int TotalSeats { get; set; }  
        public int AvailableSeats { get; set; }



        public static Event Create(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
        {
            if (totalSeats <= 0) throw new ValidationException("Общее количество мест должно быть больше 0");
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


        public void ReleaseSeats(int count = 1)
        {

            //using var transaction = await _context.Database.BeginTransactionAsync();
            //var eventEntity = await _context.Events.FindAsync(eventId);
            //if (eventEntity == null || eventEntity.AvailableSeats < count)
            //    return false;

            //// Атомарное обновление через SQL
            //var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            //    "UPDATE Events SET AvailableSeats = AvailableSeats - {0} WHERE Id = {1} AND AvailableSeats >= {0}",
            //    count, eventId);

            //if (rowsAffected > 0)
            //{
            //    await transaction.CommitAsync();
            //    return true;
            //}
            //else
            //{
            //    await transaction.RollbackAsync();
            //    return false;
            //}

            //для освобождения мест (на будущее, при отклонении брони).
             
            if((AvailableSeats + count) > TotalSeats)
            {
                throw new InvalidOperationException("Значение превышает количество мест");
            }
            AvailableSeats += count;
        }

        public bool TryReserveSeats(int count = 1)
        { 
            //  возвращает false, если свободных мест недостаточно;
            //  уменьшает AvailableSeats на count и возвращает true, если места есть. 
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
