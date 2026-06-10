using AutoMapper;


namespace Sprint1_Project_ASP_NetCore_API.ProfilesAndConfigs;


public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Простой маппинг: все свойства с одинаковыми именами сопоставятся автоматически
       // CreateMap<Product, ProductDto>();

        // Маппинг с настройкой: поле Age в DTO вычисляется из даты рождения
       // CreateMap<User, UserDto>()
          //  .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.DateOfBirth)));
    }

    private int CalculateAge(DateTime dob)
    {
        var today = DateTime.Today;
        var age = today.Year - dob.Year;
        if (dob.Date > today.AddYears(-age)) age--;
        return age;
    }
}