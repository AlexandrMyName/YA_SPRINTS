using Sprints.Bookings.Application.Dtos.EntitiesDtos.Bookings;  
using Sprints.Bookings.Domain.Entities;
using AutoMapper;


namespace Sprints.Bookings.Application.Mapping;


public class MappingEntityProfile : Profile
{

    public MappingEntityProfile()
    { 

        CreateMap<BookingInfoDto, Booking>();
        CreateMap<IBookingInfoDto, Booking>();
        CreateMap<IBookingInfoDto, IBooking>().As<Booking>(); 
    }
}