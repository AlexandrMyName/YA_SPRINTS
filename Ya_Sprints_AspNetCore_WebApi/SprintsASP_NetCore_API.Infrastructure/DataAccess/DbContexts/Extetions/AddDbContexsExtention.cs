

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts;


namespace SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts.Extetions;


public static class AddDbContexsExtention
{

    /// <summary>
    /// Добавляет контексты Баз Данных
    /// </summary>
    /// <param name="services"></param>
    /// <param name="builder"></param>
    /// <param name="useInMemoryEF"></param>
    /// <returns></returns>
    public static IServiceCollection AddDbContexts(this IServiceCollection services, WebApplicationBuilder builder, bool useInMemoryEF = false)
    {
        if (useInMemoryEF)
        {
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TestDb"));
        }
        else
        {

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        }
            
        return services;
    }
}
