using Testcontainers.PostgreSql;

namespace ClientBooking.Tests.Integration.Setup;

public class SharedContainerFixture : IAsyncLifetime
{
    public static PostgreSqlContainer DatabaseContainer { get; }

    static SharedContainerFixture()
    {
        DatabaseContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .Build();

        DatabaseContainer.StartAsync().GetAwaiter().GetResult();
    }

    public async Task InitializeAsync() => await Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await DatabaseContainer.StopAsync();
    }
}