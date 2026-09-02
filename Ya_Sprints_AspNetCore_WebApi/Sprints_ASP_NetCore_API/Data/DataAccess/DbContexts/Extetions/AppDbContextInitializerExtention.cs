using Microsoft.EntityFrameworkCore;


namespace SprintASP_NetCore_API.Data.DataAccess.DbContexts.Extetions;


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
