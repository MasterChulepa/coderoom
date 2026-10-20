using CodeRoom.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeRoom.Api.IntegrationTests;

public class CodeRoomApiFactoryTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{

    [Fact]
    public void Check_Connection_String()
    {

        using var factory = new CodeRoomApiFactory(fixture.ConnectionString);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CodeRoomDbContext>();
        var actualConnectionString = dbContext.Database.GetConnectionString();
        var expectedConnectionString = fixture.ConnectionString;

        Assert.Equal(expectedConnectionString, actualConnectionString);
    }

    [Fact]
    public async Task Migrations_Create_Project_Schema()
    {
        using var factory = new CodeRoomApiFactory(fixture.ConnectionString);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CodeRoomDbContext>();

        await dbContext.Database.MigrateAsync();

        var projectCount = await dbContext.Projects.CountAsync();

        Assert.Equal(0, projectCount);
    }
}