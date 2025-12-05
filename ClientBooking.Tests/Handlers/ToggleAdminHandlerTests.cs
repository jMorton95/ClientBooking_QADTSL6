using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Shared.Enums;
using ClientBooking.Features.ToggleAdmin;
using ClientBooking.Authentication;
using ClientBooking.Features;
using ClientBooking.Tests.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class ToggleAdminHandlerTests : UnitTestContext
{
    private readonly Mock<ISessionStateManager> _sessionMock = new();
    private readonly Mock<ILogger<ToggleAdminHandler>> _loggerMock = new();
    
    // Missing session
    [Fact]
    public async Task Handler_NoSession_ReturnsBadRequest()
    {
        var db = CreateInMemoryContext();
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns((int?)null);

        var req = new ToggleAdminHandler.Request(true);
        var result = await ToggleAdminHandler.Handler(req, _sessionMock.Object, db, _loggerMock.Object);

        Assert.IsType<BadRequest<string>>(result.Result);
    }

    // Assign admin (role exists)
    [Fact]
    public async Task Handler_AssignAdmin_RoleExists_AddsUserRole()
    {
        var db = await SeedUser(CreateInMemoryContext());
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(1000);

        // Seed admin role
        var admin = new Role { Name = RoleName.Admin };
        db.Roles.Add(admin);
        await db.SaveChangesAsync();

        var req = new ToggleAdminHandler.Request(true);
        var result = await ToggleAdminHandler.Handler(req, _sessionMock.Object, db, _loggerMock.Object);

        Assert.IsType<HtmxRedirectResult>(result.Result);

        var userRole = await db.UserRoles.FirstOrDefaultAsync(x => x.UserId == 1000);
        Assert.NotNull(userRole);
        Assert.Equal(admin.Id, userRole.RoleId);

        _sessionMock.Verify(s => s.RefreshUserSession(db), Times.Once);
    }

    // Assign admin (role does NOT exist → auto-create)
    [Fact]
    public async Task Handler_AssignAdmin_RoleDoesNotExist_CreatesRole()
    {
        var db = await SeedUser(CreateInMemoryContext());
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(1000);

        var req = new ToggleAdminHandler.Request(true);
        var result = await ToggleAdminHandler.Handler(req, _sessionMock.Object, db, _loggerMock.Object);

        Assert.IsType<HtmxRedirectResult>(result.Result);

        var adminRole = await db.Roles.FirstOrDefaultAsync(x => x.Name == RoleName.Admin);
        Assert.NotNull(adminRole);

        var userRole = await db.UserRoles.FirstOrDefaultAsync(x => x.UserId == 1000);
        Assert.NotNull(userRole);
    }

    [Fact]
    public async Task Handler_RemoveAdmin_RemovesUserRole()
    {
        var db = await SeedUser(CreateInMemoryContext());
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(1000);

        var admin = new Role { Name = RoleName.Admin };
        db.Roles.Add(admin);
        await db.SaveChangesAsync();

        db.UserRoles.Add(new UserRole { UserId = 1000, RoleId = admin.Id });
        await db.SaveChangesAsync();

        var req = new ToggleAdminHandler.Request(false);
        var result = await ToggleAdminHandler.Handler(req, _sessionMock.Object, db, _loggerMock.Object);

        Assert.IsType<HtmxRedirectResult>(result.Result);

        var deleted = await db.UserRoles.FirstOrDefaultAsync(x => x.UserId == 1000);
        Assert.Null(deleted);
    }

    // Remove admin → user does NOT have role
    [Fact]
    public async Task Handler_RemoveAdmin_NoExistingRole_DoesNothing()
    {
        var db = await SeedUser(CreateInMemoryContext());
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(1000);

        db.Roles.Add(new Role { Name = RoleName.Admin });
        await db.SaveChangesAsync();

        var req = new ToggleAdminHandler.Request(false);
        var result = await ToggleAdminHandler.Handler(req, _sessionMock.Object, db, _loggerMock.Object);

        Assert.IsType<HtmxRedirectResult>(result.Result);

        Assert.Empty(db.UserRoles);
    }

    // SaveChanges throws → returns BadRequest
    [Fact]
    public async Task Handler_SaveChangesThrows_ReturnsBadRequest()
    {
        // Shared DB name between good + faulty contexts
        var goodDb = CreateInMemoryContext();
        var faultyDb = CreateFaultyDataContext();

        // Seed user + role into the real db so faulty context "sees" them
        await SeedUser(goodDb);
        goodDb.Roles.Add(new Role { Name = RoleName.Admin });
        await goodDb.SaveChangesAsync();

        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(1000);

        var req = new ToggleAdminHandler.Request(true);
        var result = await ToggleAdminHandler.Handler(req, _sessionMock.Object, faultyDb, _loggerMock.Object);

        var bad = Assert.IsType<BadRequest<string>>(result.Result);
        Assert.Contains("DB failure", bad.Value!);
    }
}
