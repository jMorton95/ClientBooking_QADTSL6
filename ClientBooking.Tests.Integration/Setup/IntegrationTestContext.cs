using ClientBooking.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ClientBooking.Tests.Integration.Setup;

[CollectionDefinition("Integration Test Collection")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestContext>;

public class IntegrationTestContext : IAsyncLifetime
{
    private readonly IServiceScope _scope;
    public DataContext Db { get; }
    public HttpClient HttpClient { get; }

    public IntegrationTestContext()
    {
        var factory = new IntegrationTestApplicationFactory();
        HttpClient = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        Db = _scope.ServiceProvider.GetRequiredService<DataContext>();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        HttpClient.Dispose();
        return Db.DisposeAsync().AsTask();
    }
}
