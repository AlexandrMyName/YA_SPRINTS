using Testcontainers.PostgreSql;
using Xunit;


namespace SprintASP_NetCore_API.IntegrationTests.Fixture;


public class DatabaseFixture : IAsyncLifetime
{

    public PostgreSqlContainer Container { get; private set; }

    public DatabaseFixture()
    {
        Container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .Build();
    }

 
    public async ValueTask InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Container.StopAsync();
        await Container.DisposeAsync();
    }
}

[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // Этот класс просто связывает атрибут [Collection] с фикстурой
}