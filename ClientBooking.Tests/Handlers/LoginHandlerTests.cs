using ClientBooking.Authentication;
using ClientBooking.Data.Entities;
using ClientBooking.Features.Login;
using ClientBooking.Tests.Setup;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class LoginHandlerTests : UnitTestContext
{
    [Fact]
    public async Task ValidateCredentialsAsync_UserNotFound_ReturnsError()
    {
        await using var context = CreateInMemoryContext();
        var passwordHelper = CreatePasswordHelper();
        var loggerMock = new Mock<ILogger<LoginHandler>>().Object;

        var (user, error) = await LoginHandler.ValidateCredentialsAsync(
            "nonexistent@example.com",
            "password123",
            passwordHelper,
            context,
            loggerMock
        );

        Assert.Null(user);
        Assert.Equal("User not found.", error);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WrongPassword_IncrementsFailedAttempts()
    {
        await using var context = CreateInMemoryContext();

        var passwordHelper = CreatePasswordHelper();

        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            HashedPassword = passwordHelper.HashPassword("correctpassword"),
            AccessFailedCount = 0,
            IsLockedOut = false
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<LoginHandler>>().Object;

        var (resultUser, error) = await LoginHandler.ValidateCredentialsAsync(
            user.Email,
            "wrongpassword",
            passwordHelper,
            context,
            loggerMock
        );

        Assert.Equal(user, resultUser);
        Assert.Equal(1, user.AccessFailedCount);
        Assert.False(user.IsLockedOut);
        Assert.Equal("Incorrect password.", error);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_LockedOutUser_ReturnsLockedOutError()
    {
        await using var context = CreateInMemoryContext();

        var passwordHelper = CreatePasswordHelper();

        var user = new User
        {
            FirstName = "Locked",
            LastName = "User",
            Email = "locked@example.com",
            HashedPassword = passwordHelper.HashPassword("anyPassword"),
            AccessFailedCount = 5,
            IsLockedOut = true,
            LockoutEnd = DateTime.UtcNow.AddMinutes(10)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<LoginHandler>>().Object;

        var (resultUser, error) = await LoginHandler.ValidateCredentialsAsync(
            user.Email,
            "anyPassword",
            passwordHelper,
            context,
            loggerMock
        );

        Assert.Equal(user, resultUser);
        Assert.Contains("account has been locked", error);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_SuccessfulLogin_ResetsFailedAttempts()
    {
        await using var context = CreateInMemoryContext();

        var passwordHelper = CreatePasswordHelper();

        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            HashedPassword = passwordHelper.HashPassword("correctpassword"),
            AccessFailedCount = 2,
            IsLockedOut = true,
            LockoutEnd = DateTime.UtcNow.AddMinutes(-5)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<LoginHandler>>().Object;

        var (resultUser, error) = await LoginHandler.ValidateCredentialsAsync(
            user.Email,
            "correctpassword",
            passwordHelper,
            context,
            loggerMock
        );

        Assert.Equal(user, resultUser);
        Assert.Equal(0, user.AccessFailedCount);
        Assert.False(user.IsLockedOut);
        Assert.Null(error);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_UserNotFound_NoPasswordCheckCalled()
    {
        await using var context = CreateInMemoryContext();

        var passwordHelperMock = new Mock<IPasswordHelper>();
        var loggerMock = new Mock<ILogger<LoginHandler>>().Object;

        var (user, error) = await LoginHandler.ValidateCredentialsAsync(
            "nonexistent@example.com",
            "password",
            passwordHelperMock.Object,
            context,
            loggerMock
        );

        Assert.Null(user);
        Assert.Equal("User not found.", error);
        passwordHelperMock.Verify(ph => ph.CheckPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}