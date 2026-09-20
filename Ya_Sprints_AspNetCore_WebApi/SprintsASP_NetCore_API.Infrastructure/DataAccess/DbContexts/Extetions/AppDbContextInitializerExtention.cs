using Microsoft.EntityFrameworkCore;
using SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts;


namespace SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts.Extetions;


public static class AppDbContextInitializerExtention
{ 
    
    /// <summary>
    /// Инициализирует контексты Баз Данных
    /// </summary>
    /// <param name="services"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplication InitializeDataBases(this WebApplication app){

        using (var scope = app.Services.CreateScope()){

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();  //  создаёт схему и структуру Базы (миграции) 
        } 
        return app;
    }
}
