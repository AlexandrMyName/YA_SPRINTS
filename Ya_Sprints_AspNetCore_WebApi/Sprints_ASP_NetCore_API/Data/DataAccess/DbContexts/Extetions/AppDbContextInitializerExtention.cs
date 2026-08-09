using dataBase_autoMigration_Lib;


namespace SprintASP_NetCore_API.Data.DataAccess.DbContexts.Extetions;


public static class AppDbContextInitializerExtention
{ 
    
    /// <summary>
   /// Инициализирует контексты Баз Данных
   /// </summary>
   /// <param name="services"></param>
   /// <param name="builder"></param>
   /// <returns></returns>
    public static WebApplication InitializeDataBases(this WebApplication app  )
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();  //  создаёт схему и структуру Базы (если она не существует)
            db.CreateOrUpdateMigration(); //  метод пытается добавить колонки , если есть в Entity , но в базе нет
        }

        return app;
    }
}
