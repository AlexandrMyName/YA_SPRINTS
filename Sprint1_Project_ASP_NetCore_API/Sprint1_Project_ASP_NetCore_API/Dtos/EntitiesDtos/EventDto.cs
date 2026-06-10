using System.ComponentModel.DataAnnotations;

namespace Sprint1_Project_ASP_NetCore_API.Dtos.EntitiesDtos;

/// <summary>
/// Модель для Entity (DTO) -> EventDto
/// </summary>
public class EventDto
{
    [Required(ErrorMessage = "Идентификатор события обязателен")]
    /// <summary>
    /// Уникальный идентификатор
    /// </summary>
    public required Guid Id { get; set; }

    [StringLength(50, ErrorMessage ="Максимальная длинна - 50  символов")]
    [Required(ErrorMessage = "Название событие обязательно к заполнению")]
    /// <summary>
    /// Название 
    /// </summary>
    public required string Title { get; set; }
     
    /// <summary>
    /// Описание  
    /// </summary>
    public string? Description { get; set; }

    [Required(ErrorMessage = "Дата начала обязательна")]
    /// <summary>
    /// Время начала события
    /// </summary>
    public required DateTime StartAt { get; set; }
    
    
    [Required(ErrorMessage = "Дата окончания обязательна")]
    /// <summary>
    /// Время окончания события
    /// </summary>
    public required DateTime EndAt { get; set; }
}
