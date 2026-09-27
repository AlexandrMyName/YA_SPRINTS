using SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts;
using Microsoft.EntityFrameworkCore;


namespace Sprints_Project_ASP_NetCore_API.Presentation.Extensions;


public static class DatabaseInitExtensions
{
    /// <summary>
    /// Инициализирует базы данных: применяет миграции (relational) или создаёт схему (in-memory).
    /// </summary>
    public static WebApplication InitializeDataBases(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Database.IsRelational())
            db.Database.Migrate();
        else
            db.Database.EnsureCreated();

        return app;
    }
}