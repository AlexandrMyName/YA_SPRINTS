using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using System.ComponentModel.DataAnnotations;

namespace SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;

/// <summary>
/// Модель для Entity (DTO) -> EventDto
/// </summary>
public class EventInfoDto : IEventInfoDto
{
    /// <summary>
    /// Уникальный идентификатор
    /// </summary>
    [Required(ErrorMessage = "Идентификатор события обязателен")]

    public required Guid Id { get; set; }

    /// <summary>
    /// Название 
    /// </summary>
    [StringLength(50, ErrorMessage = "Максимальная длинна - 50  символов")]
    [Required(ErrorMessage = "Название событие обязательно к заполнению")]

    public required string Title { get; set; }

    /// <summary>
    /// Описание  
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Время начала события
    /// </summary>
    [Required(ErrorMessage = "Дата начала обязательна")]
    public required DateTime StartAt { get; set; }

    /// <summary>
    /// Время окончания события
    /// </summary>
    [Required(ErrorMessage = "Дата окончания обязательна")]
    public required DateTime EndAt { get; set; }


    /// <summary>
    /// общее количество мест на событии
    /// </summary>
    [Required(ErrorMessage = "Общее количество мест на событии обязательно к заполнению")]
    public int TotalSeats { get; set; }


    /// <summary>
    /// общее количество мест на событии
    /// </summary>
    [Required(ErrorMessage = "Доступное количество мест на событии обязательно к заполнению")]
    public int AvailableSeats { get; set; }

}


public interface IEventInfoDto : IEntityDto
{

    Guid Id { get; set; }
    string Title { get; set; }
    string? Description { get; set; }
    DateTime StartAt { get; set; }
    DateTime EndAt { get; set; }

    int TotalSeats { get; set; }
    int AvailableSeats { get; set; }


}