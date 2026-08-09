using Sprints_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Repositories;


namespace Sprints_Project_ASP_NetCore_API.Repositories.Extenions;


public static class AddRepositoryExtention
{
    /// <summary>
    /// Добавляет репозитории в контейнер зависимостей
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {

        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
         
        //services.AddSingleton<IRepository<IEvent>, BaseInMemoryRepository<IEvent>>();
        //services.AddSingleton<IRepository<IBooking>, BaseInMemoryRepository<IBooking>>();
        return services;
    }
}
 
        // Generic   <>  без явного определения типа
        //if (serviceType.IsGenericType && serviceType.IsGenericTypeDefinition == false)
        //{
        //    var genericDef = serviceType.GetGenericTypeDefinition();
        //    if (_registrations.TryGetValue(genericDef, out var implDef))
        //    { 
        //        var typeArguments = serviceType.GetGenericArguments();
        //        var closedImpl = implDef.MakeGenericType(typeArguments); 
        //        return CreateInstance(closedImpl); (Activator.CreateInstance...)
        //    }
        //}
       