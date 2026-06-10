using Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;
using Sprint1_Project_ASP_NetCore_API.Data.Entities;


namespace Sprint1_Project_ASP_NetCore_API.Repositories.Extenions;


public static class AddRepositoryExtention
{
    /// <summary>
    /// Добавляет репозитории в контейнер зависимостей
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        
        services.AddSingleton<IRepository<Event>, BaseInMemoryRepository<Event>>(); 
        return services;
    }  
}
