
namespace SprintASP_NetCore_API.Data.Dtos.Filters
{
    public class EventFilterDto : IEntityFilter<EventFilterDto>
    {
        // Фильтры
        public string? Title { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Location { get; set; }
        public int? MinPriority { get; set; }
        public int? MaxPriority { get; set; }

        // Сортировка
        public string? SortBy { get; set; }        
        public bool SortDesc { get; set; } = false;

        // Пагинация (опционально)
        public int? Page { get; set; }
        public int? PageSize { get; set; }


        public IQueryable<EventFilterDto> Apply(IQueryable<EventFilterDto> query)
        {
             

        }
    }
}
