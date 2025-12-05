using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Features.Me;
using ClientBooking.Features.Me.UpdateUser;
using ClientBooking.Shared.Models;
using ClientBooking.Tests.Setup;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class UpdateUserHandlerTests : UnitTestContext
{
    private readonly Mock<ISessionStateManager> _sessionMock = new();
    private readonly Mock<ILogger<UpdateUserHandler>> _loggerMock = new();

    private readonly UserProfileValidator _validator = new();

    private async Task<DataContext> SeedContextWithUser()
    {
        var context = CreateInMemoryContext();

        // Add system settings
        context.Settings.Add(new Settings
        {
            Version = 1,
            DefaultWorkingHoursStart = new TimeSpan(8, 0, 0),
            DefaultWorkingHoursEnd = new TimeSpan(16, 0, 0),
            DefaultBreakTimeStart = new TimeSpan(12, 0, 0),
            DefaultBreakTimeEnd = new TimeSpan(13, 0, 0),
            DefaultUserRole = Shared.Enums.RoleName.User
        });

        // Add a user
        var user = new User
        {
            Id = 1000,
            FirstName = "John",
            LastName = "Doe",
            Email = "Test@User.com",
            HashedPassword = "",
        };
        context.Users.Add(user);

        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task GetHandler_ReturnsUserProfile()
    {
        // Arrange
        var context = await SeedContextWithUser();
        var userId = await context.Users.Select(u => u.Id).FirstAsync();
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(userId);

        // Act
        var result = await UpdateUserHandler.GetHandler(_sessionMock.Object, context, _loggerMock.Object);

        // Assert
        Assert.IsType<Results<RazorComponentResult<UpdateUserComponent>,BadRequest<string>>>(result);
        var component = result.Result as RazorComponentResult<UpdateUserComponent>;
        var parameters = component.Parameters;
        Assert.NotNull(parameters["userProfile"]);
    }

    [Fact]
    public async Task GetHandler_MissingSession_ReturnsBadRequest()
    {
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns((int?)null);
        var context = CreateInMemoryContext();

        var result = await UpdateUserHandler.GetHandler(_sessionMock.Object, context, _loggerMock.Object);

        Assert.IsType<Results<RazorComponentResult<UpdateUserComponent>,BadRequest<string>>>(result);
    }

    [Fact]
    public async Task PostHandler_ValidProfile_ReturnsSuccess()
    {
        var context = await SeedContextWithUser();
        var userId = await context.Users.Select(u => u.Id).FirstAsync();
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(userId);

        var service = new UserWorkingHoursService(context);
        var profile = new UserProfile
        {
            FirstName = "John",
            LastName = "Doe",
            UseSystemWorkingHours = true,
            UseSystemBreakTime = true
        };

        var result = await UpdateUserHandler.PostHandler(profile, _sessionMock.Object, _validator, context, service, _loggerMock.Object);

        var rc = Assert.IsType<RazorComponentResult<UpdateUserComponent>>(result);
        Assert.True(rc.Parameters.ContainsKey("userProfile"));
        Assert.True(rc.Parameters.ContainsKey("ShowSuccessMessage"));
    }

    [Fact]
    public async Task PostHandler_InvalidProfile_ReturnsValidationErrors()
    {
        var context = await SeedContextWithUser();
        var userId = await context.Users.Select(u => u.Id).FirstAsync();
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(userId);

        var service = new UserWorkingHoursService(context);
        var profile = new UserProfile
        {
            FirstName = "", // invalid
            LastName = "",  // invalid
            UseSystemWorkingHours = false,
            UseSystemBreakTime = false
        };

        var result = await UpdateUserHandler.PostHandler(profile, _sessionMock.Object, _validator, context, service, _loggerMock.Object);

        var rc = Assert.IsType<RazorComponentResult<UpdateUserComponent>>(result);
        var validationErrors = rc.Parameters["ValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(validationErrors);
        Assert.Contains("FirstName", validationErrors.Keys);
        Assert.Contains("LastName", validationErrors.Keys);
    }

    [Fact]
    public async Task ToggleWorkingHours_UpdatesFlag()
    {
        var context = await SeedContextWithUser();
        var userId = await context.Users.Select(u => u.Id).FirstAsync();
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(userId);

        var profile = new UserProfile
        {
            UseSystemWorkingHours = false,
            FirstName = "Test",
            LastName = "User"
        };
        var result = await UpdateUserHandler.ToggleWorkingHours(profile, context, _sessionMock.Object, _loggerMock.Object);

        var rc = Assert.IsType<RazorComponentResult<UpdateUserComponent>>(result);
        Assert.Equal("working-hours", rc.Parameters["Section"]);
        var updatedProfile = rc.Parameters["UserProfile"] as UserProfile;
        Assert.NotNull(updatedProfile);
        Assert.False(updatedProfile!.UseSystemWorkingHours);
    }

    [Fact]
    public async Task ToggleBreakTime_UpdatesFlag()
    {
        var context = await SeedContextWithUser();
        var userId = await context.Users.Select(u => u.Id).FirstAsync();
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(userId);

        var profile = new UserProfile
        {
            UseSystemBreakTime = false,
            FirstName = "Test",
            LastName = "User"
        };
        var result = await UpdateUserHandler.ToggleBreakTime(profile, context, _sessionMock.Object, _loggerMock.Object);

        var rc = Assert.IsType<RazorComponentResult<UpdateUserComponent>>(result);
        Assert.Equal("break-time", rc.Parameters["Section"]);
        var updatedProfile = rc.Parameters["UserProfile"] as UserProfile;
        Assert.NotNull(updatedProfile);
        Assert.False(updatedProfile!.UseSystemBreakTime);
    }
    
    [Fact]
    public async Task GetHandler_UserNotFound_ReturnsBadRequest()
    {
        // Arrange: create context with no users
        await using var context = CreateInMemoryContext();
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(9999);

        // Act
        var result = await UpdateUserHandler.GetHandler(_sessionMock.Object, context, _loggerMock.Object);

        // Assert: returned union that contains BadRequest<string>
        Assert.IsType<Results<RazorComponentResult<UpdateUserComponent>, BadRequest<string>>>(result);
        var bad = result.Result as BadRequest<string>;
        Assert.NotNull(bad);
    }
    
    [Fact]
    public async Task PostHandler_UserNotFound_ReturnsErrorMessage()
    {
        // Arrange: context with no user but session returns id
        await using var context = CreateInMemoryContext();
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(9999);

        var profile = new UserProfile { FirstName = "X", LastName = "Y" };
        var service = new UserWorkingHoursService(context);

        // Act
        var result = await UpdateUserHandler.PostHandler(profile, _sessionMock.Object, _validator, context, service, _loggerMock.Object);

        // Assert: RazorComponentResult with ErrorMessage in parameters
        var rc = Assert.IsType<RazorComponentResult<UpdateUserComponent>>(result);
        Assert.True(rc.Parameters.ContainsKey("ErrorMessage"));
        Assert.Equal("User not found.", rc.Parameters["ErrorMessage"]);
    }
    
    [Fact]
    public async Task PostHandler_UserWorkingHoursServiceReturnsValidationErrors_AggregatesAndReturnsValidationErrors()
    {
        // Arrange
        var context = await SeedContextWithUser();
        var userId = await context.Users.Select(u => u.Id).FirstAsync();
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(userId);

        // make a mock service that returns validation failure
        var failingResult = Result<UserProfile>.ValidationFailure(new Dictionary<string, string[]>
        {
            ["TotalWorkingHours"] = ["Total shift time must be at least 7 hours"]
        });

        var userWorkingHoursServiceMock = new Mock<IUserWorkingHoursService>();
        userWorkingHoursServiceMock
            .Setup(s => s.EnforceUserWorkingHoursRules(It.IsAny<UserProfile>()))
            .ReturnsAsync(failingResult);

        var profile = new UserProfile
        {
            FirstName = "John",
            LastName = "Doe",
            UseSystemWorkingHours = false,
            UseSystemBreakTime = false,
            WorkingHoursStart = new TimeOnly(9,0),
            WorkingHoursEnd = new TimeOnly(15,0),
            BreakTimeStart = new TimeOnly(12,0),
            BreakTimeEnd = new TimeOnly(12,30)
        };

        // Act
        var result = await UpdateUserHandler.PostHandler(profile, _sessionMock.Object, _validator, context, userWorkingHoursServiceMock.Object, _loggerMock.Object);

        // Assert - should return RazorComponentResult with ValidationErrors containing the aggregated entry
        var rc = Assert.IsType<RazorComponentResult<UpdateUserComponent>>(result);
        var validationErrors = rc.Parameters["ValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(validationErrors);
        Assert.True(validationErrors.ContainsKey("TotalWorkingHours"));
        Assert.Contains("Total shift time must be at least 7 hours", validationErrors["TotalWorkingHours"]);
    }
    
    [Fact]
    public async Task PostHandler_SaveChangesThrows_ReturnsErrorMessage()
    {
        // Arrange: build FaultyDataContext that will throw on SaveChangesAsync

        var mockDb = CreateInMemoryContext();
        var faultyDb = CreateFaultyDataContext();

        // seed user into the underlying DB of the faulty context by using a regular context and same DB name
        // (we need the user to exist so the handler will attempt SaveChanges)
       
        mockDb.Settings.Add(new Settings
            {
                Version = 1,
                DefaultWorkingHoursStart = new TimeSpan(8, 0, 0),
                DefaultWorkingHoursEnd = new TimeSpan(16, 0, 0),
                DefaultBreakTimeStart = new TimeSpan(12, 0, 0),
                DefaultBreakTimeEnd = new TimeSpan(13, 0, 0),
                DefaultUserRole = Shared.Enums.RoleName.User
            });

        mockDb.Users.Add(new User
            {
                Id = 5000,
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com",
                HashedPassword = ""
            });
        
        await mockDb.SaveChangesAsync();
        
        _sessionMock.Setup(s => s.GetUserSessionId()).Returns(5000);

        var service = new UserWorkingHoursService(faultyDb); // service uses faulty context but it's ok; SaveChanges will throw

        var profile = new UserProfile
        {
            FirstName = "Jane",
            LastName = "Doe",
            UseSystemWorkingHours = true,
            UseSystemBreakTime = true
        };

        // Act
        var result = await UpdateUserHandler.PostHandler(profile, _sessionMock.Object, _validator, faultyDb, service, _loggerMock.Object);

        // Assert: should return a RazorComponentResult with ErrorMessage set to exception message
        var rc = Assert.IsType<RazorComponentResult<UpdateUserComponent>>(result);
        Assert.True(rc.Parameters.ContainsKey("ErrorMessage"));
        var msg = rc.Parameters["ErrorMessage"] as string;
        Assert.NotNull(msg);
        Assert.Contains("DB failure", msg);
    }
}