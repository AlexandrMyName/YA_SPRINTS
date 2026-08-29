using SprintASP_NetCore_API.Data.DataAccess.DbContexts;
using SprintASP_NetCore_API.IntegrationTests.Fixture;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;


namespace SprintASP_NetCore_API.IntegrationTests;


public abstract class TestBase : IAsyncLifetime
{

    protected AppDbContext DbContext { get; private set; }
    protected IServiceProvider ServiceProvider { get; private set; }
    private readonly DatabaseFixture _fixture;

 
    protected TestBase(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public virtual async ValueTask InitializeAsync()
    {
        // контейнер из фикстуры
        var connectionString = _fixture.Container.GetConnectionString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        DbContext = new AppDbContext(options);

        // Создаём схему через EnsureCreated (или миграции, если нужно)
        await DbContext.Database.EnsureCreatedAsync();

        // Настраиваем DI
        var services = new ServiceCollection();
        services.AddScoped<AppDbContext>(_ => DbContext);
        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        ServiceProvider = services.BuildServiceProvider();

        // Очищаем базу перед тестами
        await ClearDatabaseAsync();
    }

    public virtual async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync(); 
    }

    protected T GetService<T>() => ServiceProvider.GetRequiredService<T>();

    protected async Task ClearDatabaseAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"events\" RESTART IDENTITY CASCADE;");
        await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"bookings\" RESTART IDENTITY CASCADE;");
    }
}