using Sprints_Project_ASP_NetCore_API.Application.Dtos.EntitiesDtos;
using SprintASP_NetCore_API.Domain.Entities;


namespace SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Bookings;


public class BookingInfoDto : IBookingInfoDto
{

    /// <summary>
    /// Идентификатор бронирования
    /// </summary>
    public Guid Id { get; set; }
     
    /// <summary>
    /// Идентификатор пользователя 
    /// (осуществивший бронирование)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Идентификатор события
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Статус обработки
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Дата и время создания
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

   
}

public interface IBookingInfoDto : IEntityDto
{
   
    /// <summary>
    /// Идентификатор пользователя 
    /// (осуществивший бронирование)
    /// </summary>
    Guid UserId { get; set; }

    /// <summary>
    /// ID события
    /// </summary>
    Guid EventId { get; set; }

    /// <summary>
    /// Статус обработки
    /// </summary>
    BookingStatus Status { get; set; }

    /// <summary>
    /// Дата и время создания
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки
    /// </summary>
    DateTime? ProcessedAt { get; set; }
}
