using Sprints.Bookings.Application.Dtos.EntitiesDtos.Bookings; 
using Sprints.Bookings.Domain.Entities;
using AutoMapper;


namespace Sprints.Bookings.Application.Mapping;


public class MappingDtoProfile : Profile
{
    public MappingDtoProfile()
    { 

        // Booking: Entity → DTO  
        CreateMap<Booking, BookingInfoDto>();
        CreateMap<Booking, IBookingInfoDto>().As<BookingInfoDto>();
         
    }
}