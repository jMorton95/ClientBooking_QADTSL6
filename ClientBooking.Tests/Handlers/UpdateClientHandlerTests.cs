using ClientBooking.Data.Entities;
using ClientBooking.Features.Clients;
using ClientBooking.Features.Clients.Update;
using ClientBooking.Tests.Setup;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class UpdateClientHandlerTests : UnitTestContext
{
    #region GetHandler Tests

    [Fact]
    public async Task GetHandler_ClientExists_ReturnsClientRequest()
    {
        await using var db = CreateInMemoryContext();

        var client = new Client
        {
            Name = "Alice",
            Email = "alice@test.com",
            Description = "Test client"
        };
        await db.Clients.AddAsync(client);
        await db.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<UpdateClientHandler>>().Object;

        var result = await UpdateClientHandler.GetHandler(client.Id, db, loggerMock);

        var rc = Assert.IsType<RazorComponentResult<UpdateClientComponent>>(result);
        var request = rc.Parameters["clientRequest"] as ClientRequest;
        Assert.NotNull(request);
        Assert.Equal("Alice", request!.Name);
        Assert.Equal("alice@test.com", request.Email);
    }

    [Fact]
    public async Task GetHandler_ClientNotFound_ReturnsClientNotFound()
    {
        await using var db = CreateInMemoryContext();

        var loggerMock = new Mock<ILogger<UpdateClientHandler>>().Object;

        var result = await UpdateClientHandler.GetHandler(123, db, loggerMock);

        var rc = Assert.IsType<RazorComponentResult<UpdateClientComponent>>(result);
        Assert.True(rc.Parameters.ContainsKey("ClientNotFound"));
        Assert.True((bool)rc.Parameters["ClientNotFound"]);
    }

    [Fact]
    public async Task GetHandler_ExceptionThrown_ReturnsErrorMessage()
    {
        await using var db = CreateFaultyDataContext();
        var loggerMock = new Mock<ILogger<UpdateClientHandler>>().Object;

        var result = await UpdateClientHandler.GetHandler(1, db, loggerMock);

        var rc = Assert.IsType<RazorComponentResult<UpdateClientComponent>>(result);
        var error = rc.Parameters["ErrorMessage"] as string;
        Assert.NotNull(error);
        Assert.Contains("An error occurred while loading the client", error);
    }

    #endregion

    #region PostHandler Tests

    [Fact]
    public async Task PostHandler_ValidUpdate_ReturnsSuccessMessage()
    {
        await using var db = CreateInMemoryContext();

        var client = new Client
        {
            Name = "Alice",
            Email = "alice@test.com",
            Description = "Old description"
        };
        await db.Clients.AddAsync(client);
        await db.SaveChangesAsync();

        var validatorMock = new Mock<IValidator<ClientRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ClientRequest>(), CancellationToken.None))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var loggerMock = new Mock<ILogger<UpdateClientHandler>>().Object;

        var updateRequest = new ClientRequest
        {
            Name = "Alice Updated",
            Email = "alice@test.com",
            Description = "New description"
        };

        var result = await UpdateClientHandler.PostHandler(client.Id, updateRequest, validatorMock.Object, db, loggerMock);

        var rc = Assert.IsType<RazorComponentResult<UpdateClientComponent>>(result);
        Assert.True(rc.Parameters.ContainsKey("ShowSuccessMessage"));
        Assert.True((bool)rc.Parameters["ShowSuccessMessage"]);

        var updatedClient = await db.Clients.FirstAsync(c => c.Id == client.Id);
        Assert.Equal("Alice Updated", updatedClient.Name);
        Assert.Equal("New description", updatedClient.Description);
    }

    [Fact]
    public async Task PostHandler_ValidationFails_ReturnsValidationErrors()
    {
        await using var db = CreateInMemoryContext();

        var client = new Client { Name = "Alice", Email = "alice@test.com", Description = "Desc" };
        await db.Clients.AddAsync(client);
        await db.SaveChangesAsync();

        var validatorMock = new Mock<IValidator<ClientRequest>>();
        var validationResult = new FluentValidation.Results.ValidationResult(
            new[] { new FluentValidation.Results.ValidationFailure("Name", "Required") }
        );
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ClientRequest>(), CancellationToken.None))
            .ReturnsAsync(validationResult);

        var loggerMock = new Mock<ILogger<UpdateClientHandler>>().Object;

        var updateRequest = new ClientRequest
        {
            Name = "",
            Email = "alice@test.com",
            Description = "Desc"
        };

        var result = await UpdateClientHandler.PostHandler(client.Id, updateRequest, validatorMock.Object, db, loggerMock);

        var rc = Assert.IsType<RazorComponentResult<UpdateClientComponent>>(result);
        var errors = rc.Parameters["ValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("Name"));
    }

    [Fact]
    public async Task PostHandler_ClientNotFound_ReturnsClientNotFound()
    {
        await using var db = CreateInMemoryContext();

        var validatorMock = new Mock<IValidator<ClientRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ClientRequest>(), CancellationToken.None))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var loggerMock = new Mock<ILogger<UpdateClientHandler>>().Object;

        var updateRequest = new ClientRequest { Name = "Alice", Email = "alice@test.com", Description = "Desc" };

        var result = await UpdateClientHandler.PostHandler(999, updateRequest, validatorMock.Object, db, loggerMock);

        var rc = Assert.IsType<RazorComponentResult<UpdateClientComponent>>(result);
        Assert.True(rc.Parameters.ContainsKey("ClientNotFound"));
        Assert.True((bool)rc.Parameters["ClientNotFound"]);
    }

    [Fact]
    public async Task PostHandler_EmailAlreadyExists_ReturnsErrorMessage()
    {
        await using var db = CreateInMemoryContext();

        var client1 = new Client { Name = "Alice", Email = "alice@test.com", Description = "Desc" };
        var client2 = new Client { Name = "Bob", Email = "bob@test.com", Description = "Desc" };
        await db.Clients.AddRangeAsync(client1, client2);
        await db.SaveChangesAsync();

        var validatorMock = new Mock<IValidator<ClientRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ClientRequest>(), CancellationToken.None))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var loggerMock = new Mock<ILogger<UpdateClientHandler>>().Object;

        var updateRequest = new ClientRequest { Name = "Alice Updated", Email = "bob@test.com", Description = "Desc" };

        var result = await UpdateClientHandler.PostHandler(client1.Id, updateRequest, validatorMock.Object, db, loggerMock);

        var rc = Assert.IsType<RazorComponentResult<UpdateClientComponent>>(result);
        var error = rc.Parameters["ErrorMessage"] as string;
        Assert.NotNull(error);
        Assert.Contains("already exists", error);
    }

    [Fact]
    public async Task PostHandler_ExceptionThrown_ReturnsErrorMessage()
    {
        await using var db = CreateFaultyDataContext();

        var validatorMock = new Mock<IValidator<ClientRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ClientRequest>(), CancellationToken.None))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var loggerMock = new Mock<ILogger<UpdateClientHandler>>().Object;

        var updateRequest = new ClientRequest { Name = "Alice", Email = "alice@test.com", Description = "Desc" };

        var result = await UpdateClientHandler.PostHandler(1, updateRequest, validatorMock.Object, db, loggerMock);

        var rc = Assert.IsType<RazorComponentResult<UpdateClientComponent>>(result);
        var error = rc.Parameters["ErrorMessage"] as string;
        Assert.NotNull(error);
        Assert.Contains("Error occurred updating client", error);
    }

    #endregion
}
