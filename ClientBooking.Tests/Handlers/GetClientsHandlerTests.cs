using ClientBooking.Components.Generic;
using ClientBooking.Data.Entities;
using ClientBooking.Features.Clients.View;
using ClientBooking.Tests.Setup;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class GetClientsHandlerTests : UnitTestContext
{
    // Test: Successful retrieval of clients with default parameters
    [Fact]
    public async Task Handler_ReturnsClients_DefaultParameters()
    {
        await using var db = CreateInMemoryContext();

        // Arrange: seed some clients
        var clients = new List<Client>();
        for (int i = 1; i <= 10; i++)
        {
            clients.Add(new Client
            {
                Name = $"Client {i}",
                Email = $"client{i}@test.com",
                Description = $"Description {i}"
            });
        }
        await db.Clients.AddRangeAsync(clients);
        await db.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GetClientsHandler>>().Object;

        // Act
        var result = await GetClientsHandler.GetHandler(db, loggerMock);

        // Assert outer Results type
        Assert.IsType<Results<RazorComponentResult<ClientsComponent>, RazorComponentResult<ErrorMessageComponent>>>(result);

        // Assert returned RazorComponentResult<ClientsComponent>
        var rc = result.Result as RazorComponentResult<ClientsComponent>;
        Assert.NotNull(rc);

        var returnedClients = rc.Parameters["Clients"] as List<Client>;
        Assert.NotNull(returnedClients);
        Assert.Equal(GetClientsHandler.ClientsPagePageSize, returnedClients!.Count);
        Assert.Equal("Client 1", returnedClients.First().Name);
    }

    // Test: Sorting by email descending
    [Fact]
    public async Task Handler_ReturnsClients_SortedByEmailDesc()
    {
        await using var db = CreateInMemoryContext();

        var clients = new List<Client>
        {
            new() { Name = "Alice", Email = "alice@test.com", Description = "Desc1" },
            new() { Name = "Bob", Email = "bob@test.com", Description = "Desc2" },
            new() { Name = "Charlie", Email = "charlie@test.com", Description = "Desc3" }
        };
        await db.Clients.AddRangeAsync(clients);
        await db.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GetClientsHandler>>().Object;

        var result = await GetClientsHandler.GetHandler(
            db,
            loggerMock,
            sortBy: "email",
            sortDirection: "desc"
        );

        Assert.IsType<Results<RazorComponentResult<ClientsComponent>, RazorComponentResult<ErrorMessageComponent>>>(result);

        var rc = result.Result as RazorComponentResult<ClientsComponent>;
        Assert.NotNull(rc);

        var returnedClients = rc.Parameters["Clients"] as List<Client>;
        Assert.NotNull(returnedClients);
        Assert.Equal("charlie@test.com", returnedClients!.First().Email);
        Assert.Equal("alice@test.com", returnedClients.Last().Email);
    }

    // Test: Searching by name
    [Fact]
    public async Task Handler_ReturnsClients_SearchByName()
    {
        await using var db = CreateInMemoryContext();

        List<Client> clients = [
            new() { Name = "Alice Smith", Email = "alice@test.com", Description = "Desc1" },
            new() { Name = "Bob Jones", Email = "bob@test.com", Description = "Desc2" }
        ];
        await db.Clients.AddRangeAsync(clients);
        await db.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GetClientsHandler>>().Object;

        var result = await GetClientsHandler.GetHandler(db, loggerMock, search: "Bob");

        Assert.IsType<Results<RazorComponentResult<ClientsComponent>, RazorComponentResult<ErrorMessageComponent>>>(result);

        var rc = result.Result as RazorComponentResult<ClientsComponent>;
        Assert.NotNull(rc);

        var returnedClients = rc.Parameters["Clients"] as List<Client>;
        Assert.Single(returnedClients);
        Assert.Equal("Bob Jones", returnedClients!.First().Name);
    }

    // Test: Exception during client retrieval
    [Fact]
    public async Task Handler_ExceptionThrown_ReturnsErrorMessageComponent()
    {
        await using var db = CreateFaultyDataContext();

        var loggerMock = new Mock<ILogger<GetClientsHandler>>().Object;

        var result = await GetClientsHandler.GetHandler(db, loggerMock);

        Assert.IsType<Results<RazorComponentResult<ClientsComponent>, RazorComponentResult<ErrorMessageComponent>>>(result);

        var rc = result.Result as RazorComponentResult<ErrorMessageComponent>;
        Assert.NotNull(rc);

        var errorMessage = rc.Parameters["ErrorMessage"] as string;
        Assert.NotNull(errorMessage);
        Assert.Contains("Error occurred updating client", errorMessage);
    }
}
