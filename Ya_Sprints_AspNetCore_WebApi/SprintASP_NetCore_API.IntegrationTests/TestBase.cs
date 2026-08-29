using SprintASP_NetCore_API.Data.DataAccess.DbContexts; 
using Sprints_Project_ASP_NetCore_API.Repositories;
using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;


namespace SprintASP_NetCore_API.IntegrationTests;

public abstract class TestBase : IAsyncLifetime
{

    private readonly PostgreSqlContainer _container;
    protected AppDbContext DbContext { get; private set; }
    protected IServiceProvider ServiceProvider { get; private set; }

    public TestBase()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .Build();
    }

    public virtual async ValueTask InitializeAsync()
    {
        // 1. Запускаем контейнер
        await _container.StartAsync();

        // 2. Настраиваем контекст (без миграций)
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        DbContext = new AppDbContext(options);

        // 3. СОЗДАЁМ СХЕМУ (таблицы) – БЕЗ МИГРАЦИЙ
        await DbContext.Database.EnsureCreatedAsync();

        // 4. Настраиваем DI
        var services = new ServiceCollection();
        services.AddScoped<AppDbContext>(_ => DbContext);
        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        ServiceProvider = services.BuildServiceProvider();

        // 5. Очищаем базу (таблицы уже есть)
        await ClearDatabaseAsync();
    }

    public virtual async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    protected T GetService<T>() => ServiceProvider.GetRequiredService<T>();

    protected async Task ClearDatabaseAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"bookings\" RESTART IDENTITY CASCADE;");
        await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"events\" RESTART IDENTITY CASCADE;");
    }
}