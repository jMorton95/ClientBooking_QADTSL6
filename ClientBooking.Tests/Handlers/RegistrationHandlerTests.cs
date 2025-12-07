using ClientBooking.Authentication;
using ClientBooking.Data.Entities;
using ClientBooking.Features;
using ClientBooking.Features.Registration;
using ClientBooking.Shared.Services;
using ClientBooking.Tests.Setup;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class RegistrationHandlerTests : UnitTestContext
{
    // Test: Successful registration
    [Fact]
    public async Task Handler_ValidRegistration_CreatesUserAndLogsIn()
    {
        await using var context = CreateInMemoryContext();
        var passwordHelper = CreatePasswordHelper();
        var loggerMock = new Mock<ILogger<RegistrationHandler>>().Object;
        var sessionMock = new Mock<ISessionStateManager>();
        var createUserServiceMock = new Mock<ICreateRegisteredUserService>();
        var validatorMock = new Mock<IValidator<RegistrationRequest>>();

        var requestDto = new RegistrationRequest
        {
            Email = "test@example.com",
            PasswordTwo = "password123",
            PasswordOne = "password123"
        };

        validatorMock.Setup(v => v.ValidateAsync(requestDto, CancellationToken.None))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        
        
        var newUser = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = requestDto.Email,
            HashedPassword = "",
            IsActive = true
        };

        var hashedPassword = passwordHelper.HashPassword(newUser, requestDto.PasswordTwo);
        newUser.HashedPassword = hashedPassword;

        createUserServiceMock.Setup(s => s.CreateUserWithDefaultSettings(requestDto, It.IsAny<string>()))
            .ReturnsAsync(newUser);

        var requestWrapper = new RegistrationHandler.Request(requestDto);

        var result = await RegistrationHandler.Handler(
            requestWrapper,
            validatorMock.Object,
            context,
            passwordHelper,
            sessionMock.Object,
            createUserServiceMock.Object,
            loggerMock
        );

        // Assert the union type
        Assert.NotNull(result);
        Assert.IsType<Results<HtmxRedirectResult, RazorComponentResult<RegistrationPage>, InternalServerError<string>>>(result);

        // Extract HtmxRedirectResult
        var redirectResult = result.Result as HtmxRedirectResult;
        Assert.NotNull(redirectResult);
        Assert.Equal("/", redirectResult.Url);

        createUserServiceMock.Verify(s => s.CreateUserWithDefaultSettings(requestDto, It.IsAny<string>()), Times.Once);
        sessionMock.Verify(s => s.LoginAsync(newUser, persistSession: true), Times.Once);
    }

    // Test: Validation fails
    [Fact]
    public async Task Handler_InvalidRequest_ReturnsValidationErrors()
    {
        await using var context = CreateInMemoryContext();
        var passwordHelper = CreatePasswordHelper();
        var loggerMock = new Mock<ILogger<RegistrationHandler>>().Object;
        var sessionMock = new Mock<ISessionStateManager>();
        var createUserServiceMock = new Mock<ICreateRegisteredUserService>();
        var validatorMock = new Mock<IValidator<RegistrationRequest>>();

        var requestDto = new RegistrationRequest { Email = "", PasswordTwo = "", PasswordOne = "" };

        var validationResult = new FluentValidation.Results.ValidationResult(
            [new FluentValidation.Results.ValidationFailure("Email", "Required")]
        );

        validatorMock.Setup(v => v.ValidateAsync(requestDto, CancellationToken.None)).ReturnsAsync(validationResult);

        var requestWrapper = new RegistrationHandler.Request(requestDto);

        var result = await RegistrationHandler.Handler(
            requestWrapper,
            validatorMock.Object,
            context,
            passwordHelper,
            sessionMock.Object,
            createUserServiceMock.Object,
            loggerMock
        );

        // Extract RazorComponentResult<RegistrationPage>
        var razorResult = result.Result as RazorComponentResult<RegistrationPage>;
        Assert.NotNull(razorResult);
        
        var validationErrors = razorResult.Parameters["ValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(validationErrors);
        Assert.True(validationErrors.ContainsKey("Email"));
        Assert.Contains("Required", validationErrors["Email"]);

        createUserServiceMock.Verify(s => s.CreateUserWithDefaultSettings(It.IsAny<RegistrationRequest>(), It.IsAny<string>()), Times.Never);
        sessionMock.Verify(s => s.LoginAsync(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
    }

    // Test: Email already exists
    [Fact]
    public async Task Handler_EmailAlreadyExists_ReturnsError()
    {
        await using var context = CreateInMemoryContext();
        var passwordHelper = CreatePasswordHelper();
        var loggerMock = new Mock<ILogger<RegistrationHandler>>().Object;
        var sessionMock = new Mock<ISessionStateManager>();
        var createUserServiceMock = new Mock<ICreateRegisteredUserService>();
        var validatorMock = new Mock<IValidator<RegistrationRequest>>();

        var existingUser = new User
        {
            FirstName = "Existing",
            LastName = "User",
            Email = "test@example.com",
            HashedPassword = "",
            IsActive = true
        };
        
        var password = passwordHelper.HashPassword(existingUser, "anypassword");
        existingUser.HashedPassword = password;
        
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var requestDto = new RegistrationRequest
        {
            Email = "test@example.com",
            PasswordTwo = "password123",
            PasswordOne = "password123"
        };
        validatorMock.Setup(v => v.ValidateAsync(requestDto, CancellationToken.None))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var requestWrapper = new RegistrationHandler.Request(requestDto);

        var result = await RegistrationHandler.Handler(
            requestWrapper,
            validatorMock.Object,
            context,
            passwordHelper,
            sessionMock.Object,
            createUserServiceMock.Object,
            loggerMock
        );

        // Extract RazorComponentResult<RegistrationPage>
        var razorResult = result.Result as RazorComponentResult<RegistrationPage>;
        Assert.NotNull(razorResult?.Parameters.ContainsKey("ErrorMessage"));
        
        var errorMessage = razorResult?.Parameters["ErrorMessage"] as string;
        Assert.Contains("already exists", errorMessage);

        createUserServiceMock.Verify(s => s.CreateUserWithDefaultSettings(It.IsAny<RegistrationRequest>(), It.IsAny<string>()), Times.Never);
        sessionMock.Verify(s => s.LoginAsync(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
    }

    // Test: Exception during registration
    [Fact]
    public async Task Handler_ExceptionThrown_ReturnsInternalServerError()
    {
        await using var context = CreateInMemoryContext();
        var passwordHelper = CreatePasswordHelper();
        var loggerMock = new Mock<ILogger<RegistrationHandler>>().Object;
        var sessionMock = new Mock<ISessionStateManager>();
        var createUserServiceMock = new Mock<ICreateRegisteredUserService>();
        var validatorMock = new Mock<IValidator<RegistrationRequest>>();

        var requestDto = new RegistrationRequest
        {
            Email = "test@example.com",
            PasswordTwo = "password123",
            PasswordOne = "password123"
        };

        validatorMock.Setup(v => v.ValidateAsync(requestDto, CancellationToken.None))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        createUserServiceMock.Setup(s => s.CreateUserWithDefaultSettings(requestDto, It.IsAny<string>()))
            .ThrowsAsync(new Exception("DB failure"));

        var requestWrapper = new RegistrationHandler.Request(requestDto);

        var result = await RegistrationHandler.Handler(
            requestWrapper,
            validatorMock.Object,
            context,
            passwordHelper,
            sessionMock.Object,
            createUserServiceMock.Object,
            loggerMock
        );

        Assert.NotNull(result);
        Assert.IsType<Results<HtmxRedirectResult, RazorComponentResult<RegistrationPage>, InternalServerError<string>>>(result);

        var errorResult = result.Result as InternalServerError<string>;
        Assert.NotNull(errorResult);
        Assert.Equal("DB failure", errorResult.Value);
    }
}