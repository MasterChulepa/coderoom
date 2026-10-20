using Testcontainers.PostgreSql;

namespace CodeRoom.Api.IntegrationTests;

public class PostgreSqlFixture : IAsyncLifetime
{
    public string ConnectionString => _container.GetConnectionString();
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("testdb")
        .WithUsername("postgres")
        .WithPassword("password")
        .Build();

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }
}