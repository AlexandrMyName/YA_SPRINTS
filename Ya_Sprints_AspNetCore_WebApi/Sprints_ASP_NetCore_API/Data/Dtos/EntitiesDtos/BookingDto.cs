using SprintASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;

namespace SprintASP_NetCore_API.Data.Dtos.EntitiesDtos;

public class BookingDto : IBookingDto
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

public interface IBookingDto : IEntityDto
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
