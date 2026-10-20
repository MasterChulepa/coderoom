namespace CodeRoom.Api.IntegrationTests;

public class PostgreSqlFixtureTests(PostgreSqlFixture fixture) :
    IClassFixture<PostgreSqlFixture>
{


    [Fact]
    public void ConnectionStringIsAvailable()
    {
        Assert.False(string.IsNullOrWhiteSpace(fixture.ConnectionString));
    }

}
