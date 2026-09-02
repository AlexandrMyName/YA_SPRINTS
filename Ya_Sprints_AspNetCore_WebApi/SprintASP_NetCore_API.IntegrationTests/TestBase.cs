using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.Data.DataAccess.DbContexts;
using SprintASP_NetCore_API.IntegrationTests.Fixture;
using SprintASP_NetCore_API.Repositories;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Xunit;

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

        var connectionString = _fixture.Container.GetConnectionString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        // Создаём контекст для миграций (он же будет использоваться в тестах)
        DbContext = new AppDbContext(options);
        await DbContext.Database.MigrateAsync();

        // Настраиваем DI с фабрикой, которая создаёт новый контекст для каждого scope
        var services = new ServiceCollection();
        services.AddScoped<AppDbContext>(_ => new AppDbContext(options));
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