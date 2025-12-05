using ClientBooking.Authentication;
using ClientBooking.Components.Generic;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Features.Audit;
using ClientBooking.Tests.Setup;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class AuditHandlerTests : UnitTestContext
{
    [Fact]
    public async Task GetHandler_ReturnsAuditListComponent()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        // Seed some audit logs
        context.AuditLogs.AddRange(new[]
        {
            new AuditLog { Id = 1, Action = AuditAction.Create, EntityName = "Test", EntityId = "1"},
            new AuditLog { Id = 2, Action = AuditAction.Create, EntityName = "Test", EntityId = "2" },
            new AuditLog { Id = 3, Action = AuditAction.Update, EntityName = "Test", EntityId = "2"}
        });
        await context.SaveChangesAsync();

        var sessionMock = new Mock<ISessionStateManager>();
        var loggerMock = new Mock<ILogger<AuditHandler>>().Object;

        // Act
        var result = AuditHandler.GetHandler(context, sessionMock.Object, loggerMock);

        var razorResult = await result;

        Assert.NotNull(razorResult);
        Assert.IsType<RazorComponentResult<AuditListComponent>>(razorResult);

        var parameters = razorResult.Parameters;
        Assert.True(parameters.ContainsKey("auditLogs"));
        var auditLogs = parameters["auditLogs"] as List<AuditLog>;
        Assert.NotNull(auditLogs);
        Assert.Equal(3, auditLogs.Count);
        Assert.Equal(3, (int)(parameters["totalCount"] ?? 0));
        Assert.Equal(1, (int)(parameters["totalPages"] ?? 0));
        Assert.Equal(1, (int)(parameters["pageNumber"] ?? 0));
    }

    [Fact]
    public async Task GetHandler_Exception_ReturnsErrorMessageComponent()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        var sessionMock = new Mock<ISessionStateManager>();
        
        // Wrap the context in a "faulty" proxy that throws when accessing AuditLogs
        var throwingContext = new ThrowingAuditLogsContext(sessionMock.Object);
        var loggerMock = new Mock<ILogger<AuditHandler>>().Object;

        // Act
        var result = AuditHandler.GetHandler(throwingContext, sessionMock.Object, loggerMock);
        var razorResult = await result;

        // Assert
        Assert.NotNull(razorResult);
        Assert.IsType<RazorComponentResult<ErrorMessageComponent>>(razorResult);

        var errorMessage = razorResult.Parameters["ErrorMessage"] as string;
        Assert.Equal("Error accessing user context.", errorMessage);
    }

    public class ThrowingAuditLogsContext(ISessionStateManager sessionManager)
        : DataContext(new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options, sessionManager)
    {
        public override DbSet<AuditLog> AuditLogs => throw new Exception("DB failure");
    }
}