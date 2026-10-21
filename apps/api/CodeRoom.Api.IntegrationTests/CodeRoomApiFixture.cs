using CodeRoom.Api.Collaboration;
using CodeRoom.Api.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeRoom.Api.IntegrationTests;

public class CodeRoomApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlFixture _db = new();
    private CodeRoomApiFactory? _factory;

    private HttpClient? _client;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Client is not initialized. Ensure InitializeAsync has been called.");

    public IHubContext<ProjectHub> HubContext => _factory?.Services.GetRequiredService<IHubContext<ProjectHub>>() ?? throw new InvalidOperationException("HubContext is not initialized. Ensure InitializeAsync has been called.");
    public string ConnectionString => _db.ConnectionString;

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        _factory = new CodeRoomApiFactory(_db.ConnectionString);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CodeRoomDbContext>();
            await dbContext.Database.MigrateAsync();
        }
        _client = _factory.CreateClient();
        _client.BaseAddress = new Uri("https://localhost");
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        return _db.DisposeAsync();
    }

}