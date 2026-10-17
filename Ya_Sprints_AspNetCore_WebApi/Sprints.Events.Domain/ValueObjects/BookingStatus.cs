using System.ComponentModel; 


namespace Sprints.Events.Domain.ValueObjects;


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
    /// <summary>
    /// бронь отменена
    /// </summary>
    [Description("Бронь отменена")]
    Cancelled,
}
