using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Events;  
using SprintASP_NetCore_API.Domain.Entities;
using AutoMapper;



namespace SprintASP_NetCore_API.Application.Mapping;


public class MappingDtoProfile : Profile
{
    public MappingDtoProfile()
    {

        // Event: Entity → DTO  
        CreateMap<Event, EventInfoDto>();
        CreateMap<Event, IEventInfoDto>().As<EventInfoDto>();    

        CreateMap<CreateEventDto, Event>()
            .ForMember(dest => dest.AvailableSeats, opt => opt.MapFrom(src => src.TotalSeats));

        CreateMap<CreateEventDto, EventInfoDto>();

        // Booking: Entity → DTO  
        CreateMap<Booking, BookingInfoDto>();
        CreateMap<Booking, IBookingInfoDto>().As<BookingInfoDto>();  
    }
}