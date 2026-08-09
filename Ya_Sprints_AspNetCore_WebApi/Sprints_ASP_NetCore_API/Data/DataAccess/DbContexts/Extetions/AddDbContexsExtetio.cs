

using Microsoft.EntityFrameworkCore;

namespace SprintASP_NetCore_API.Data.DataAccess.DbContexts.Extetions;

public static class AddDbContexsExtetio
{
    /// <summary>
    /// Добавляет контексты Баз Данных
    /// </summary>
    /// <param name="services"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IServiceCollection AddDbContexts(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        return services;
    }
}
