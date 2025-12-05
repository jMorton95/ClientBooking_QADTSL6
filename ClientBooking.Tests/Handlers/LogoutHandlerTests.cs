using ClientBooking.Authentication;
using ClientBooking.Features.Logout;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class LogoutHandlerTests
{
    [Fact]
    public void Handle_LogsOutAndRedirects()
    {
        // Arrange
        var sessionMock = new Mock<ISessionStateManager>();

        // Act
        var result = LogoutHandler.Handle(sessionMock.Object);

        // Assert that logout was called
        sessionMock.Verify(s => s.LogoutAsync(), Times.Once);

        // Assert the result is a redirect
        var redirectResult = result;
        Assert.NotNull(redirectResult);
        Assert.Equal("/login", redirectResult.Url);
    }
}