using ClientBooking.Data.Entities;
using ClientBooking.Features;
using ClientBooking.Features.Clients;
using ClientBooking.Features.Clients.Create;
using ClientBooking.Tests.Setup;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class CreateClientHandlerTests : UnitTestContext
{
    // Test: Successful client creation
    [Fact]
    public async Task Handler_ValidClient_CreatesClientAndRedirects()
    {
        await using var db = CreateInMemoryContext();

        var loggerMock = new Mock<ILogger<CreateClientHandler>>().Object;
        var validatorMock = new Mock<IValidator<ClientRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ClientRequest>(), CancellationToken.None))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var clientRequest = new ClientRequest
        {
            Name = "Alice",
            Email = "alice@test.com",
            Description = "Test client"
        };

        var result = await CreateClientHandler.Handler(clientRequest, validatorMock.Object, db, loggerMock);

        // Assert union type
        Assert.IsType<Results<HtmxRedirectResult, RazorComponentResult<CreateClientPage>>>(result);

        var redirectResult = result.Result as HtmxRedirectResult;
        Assert.NotNull(redirectResult);
        Assert.Equal("/clients", redirectResult.Url);

        // Assert client was saved
        var savedClient = await db.Clients.FirstOrDefaultAsync(c => c.Email == "alice@test.com");
        Assert.NotNull(savedClient);
        Assert.Equal("Alice", savedClient!.Name);
        Assert.Equal("Test client", savedClient.Description);
    }

    // Test: Validation fails
    [Fact]
    public async Task Handler_InvalidClient_ReturnsValidationErrors()
    {
        await using var db = CreateInMemoryContext();

        var loggerMock = new Mock<ILogger<CreateClientHandler>>().Object;
        var validatorMock = new Mock<IValidator<ClientRequest>>();

        var validationResult = new FluentValidation.Results.ValidationResult(
            new[] { new FluentValidation.Results.ValidationFailure("Email", "Required") }
        );

        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ClientRequest>(), CancellationToken.None))
            .ReturnsAsync(validationResult);

        var clientRequest = new ClientRequest
        {
            Name = "",
            Email = "",
            Description = ""
        };

        var result = await CreateClientHandler.Handler(clientRequest, validatorMock.Object, db, loggerMock);

        var rc = result.Result as RazorComponentResult<CreateClientPage>;
        Assert.NotNull(rc);

        var errors = rc.Parameters["ValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("Email"));
        Assert.Contains("Required", errors["Email"]);
    }

    // Test: Email already exists
    [Fact]
    public async Task Handler_EmailAlreadyExists_ReturnsErrorMessage()
    {
        await using var db = CreateInMemoryContext();

        var existingClient = new Client
        {
            Name = "Bob",
            Email = "bob@test.com",
            Description = "Existing client"
        };
        await db.Clients.AddAsync(existingClient);
        await db.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<CreateClientHandler>>().Object;
        var validatorMock = new Mock<IValidator<ClientRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ClientRequest>(), CancellationToken.None))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var clientRequest = new ClientRequest
        {
            Name = "Bob Duplicate",
            Email = "bob@test.com",
            Description = "Duplicate"
        };

        var result = await CreateClientHandler.Handler(clientRequest, validatorMock.Object, db, loggerMock);

        var rc = result.Result as RazorComponentResult<CreateClientPage>;
        Assert.NotNull(rc);

        var errorMessage = rc.Parameters["ErrorMessage"] as string;
        Assert.NotNull(errorMessage);
        Assert.Contains("already exists", errorMessage);
    }

    // Test: Exception during creation
    [Fact]
    public async Task Handler_ExceptionThrown_ReturnsErrorMessage()
    {
        await using var db = CreateFaultyDataContext(); // context that throws on SaveChangesAsync

        var loggerMock = new Mock<ILogger<CreateClientHandler>>().Object;
        var validatorMock = new Mock<IValidator<ClientRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ClientRequest>(), CancellationToken.None))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var clientRequest = new ClientRequest
        {
            Name = "Alice",
            Email = "alice@test.com",
            Description = "Test client"
        };

        var result = await CreateClientHandler.Handler(clientRequest, validatorMock.Object, db, loggerMock);

        var rc = result.Result as RazorComponentResult<CreateClientPage>;
        Assert.NotNull(rc);

        var errorMessage = rc.Parameters["ErrorMessage"] as string;
        Assert.NotNull(errorMessage);
        Assert.Contains("DB failure", errorMessage);
    }
}
