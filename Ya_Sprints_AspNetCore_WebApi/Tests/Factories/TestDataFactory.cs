using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;


namespace Tests.Factories
{

    public static class TestDataFactory
    {

        public static EventInfoDto CreateEventDto(
            string title = "Test Event",
            DateTime? startAt = null,
            DateTime? endAt = null)
        {
            var now = DateTime.Now;

            return new EventInfoDto
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = "Test Description",
                StartAt = startAt ?? now,
                EndAt = endAt ?? now.AddHours(1)
            };
        }


        public static EventFilterDto CreateFilter(
            string? title = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            return new EventFilterDto
            {
                Title = title,
                From = from,
                To = to,
                Page = 1,
                PageSize = 10
            };
        }


        public static List<EventInfoDto> CreateEventList(int count = 3)
        {
            var events = new List<EventInfoDto>();
            for (int i = 0; i < count; i++)
            {
                events.Add(CreateEventDto($"Event {i + 1}"));
            }
            return events;
        }
    }
}