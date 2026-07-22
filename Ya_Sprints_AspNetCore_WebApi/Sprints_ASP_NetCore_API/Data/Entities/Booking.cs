using Sprints_Project_ASP_NetCore_API.Data.Entities;
using System.ComponentModel;


namespace SprintASP_NetCore_API.Data.Entities;


public interface IBooking : IEntity
{ 

    /// <summary>
    /// ID события
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

/// <summary>
/// Объект бронирования
/// </summary>
public class Booking : IBooking
{

    /// <summary>
    /// ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID события
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


public enum BookingStatus
{
    /// <summary>
    /// бронь создана, ожидает обработки
    /// </summary>
    [Description("Бронь создана, ожидает обработки")]
    Pending,
    /// <summary>
    /// бронь подтверждена
    /// </summary>
    [Description("Бронь подтверждена")]
    Confirmed,
    /// <summary>
    /// бронь отклонена
    /// </summary>
    [Description("Бронь отклонена")]
    Rejected,
}
