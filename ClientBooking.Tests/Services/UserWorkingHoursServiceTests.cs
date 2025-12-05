using ClientBooking.Data.Entities;
using ClientBooking.Features.Me;
using ClientBooking.Shared.Models;
using ClientBooking.Tests.Setup;

namespace ClientBooking.Tests.Services;

public class UserWorkingHoursServiceTests : UnitTestContext
{
    [Fact]
    public async Task EnforceUserWorkingHoursRules_ValidHours_NoSystemOverride_ReturnsSuccess()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        var userProfile = new UserProfile
        {
            FirstName = "Test",
            LastName = "User",
            WorkingHoursStart = new TimeOnly(9, 0),
            WorkingHoursEnd = new TimeOnly(17, 0),
            BreakTimeStart = new TimeOnly(12, 0),
            BreakTimeEnd = new TimeOnly(13, 0),
            UseSystemWorkingHours = false,
            UseSystemBreakTime = false
        };

        var service = new UserWorkingHoursService(context);

        // Act
        var result = await service.EnforceUserWorkingHoursRules(userProfile);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userProfile, result.Value);
        Assert.Empty(service.ValidateEffectiveHours(userProfile));
    }

    [Fact]
    public async Task EnforceUserWorkingHoursRules_SystemOverride_AppliesSystemSettings()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        // Seed system settings
        context.Settings.Add(new Settings
        {
            Version = 1,
            DefaultWorkingHoursStart = new TimeSpan(8, 0, 0),
            DefaultWorkingHoursEnd = new TimeSpan(16, 0, 0),
            DefaultBreakTimeStart = new TimeSpan(11, 0, 0),
            DefaultBreakTimeEnd = new TimeSpan(12, 0, 0),
            DefaultUserRole = Shared.Enums.RoleName.User
        });
        await context.SaveChangesAsync();

        var userProfile = new UserProfile
        {
            FirstName = "Test",
            LastName = "User",
            WorkingHoursStart = new TimeOnly(9, 0),
            WorkingHoursEnd = new TimeOnly(17, 0),
            BreakTimeStart = new TimeOnly(12, 0),
            BreakTimeEnd = new TimeOnly(13, 0),
            UseSystemWorkingHours = true,
            UseSystemBreakTime = true
        };

        var service = new UserWorkingHoursService(context);

        // Act
        var result = await service.EnforceUserWorkingHoursRules(userProfile);

        // Assert
        Assert.True(result.IsSuccess);
        // Effective hours should match system settings
        Assert.Equal(new TimeOnly(8, 0), userProfile.WorkingHoursStart);
        Assert.Equal(new TimeOnly(16, 0), userProfile.WorkingHoursEnd);
        Assert.Equal(new TimeOnly(11, 0), userProfile.BreakTimeStart);
        Assert.Equal(new TimeOnly(12, 0), userProfile.BreakTimeEnd);
    }

    [Fact]
    public async Task EnforceUserWorkingHoursRules_InvalidBreakRange_ReturnsValidationFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        var userProfile = new UserProfile
        {
            FirstName = "Test",
            LastName = "User",
            WorkingHoursStart = new TimeOnly(9, 0),
            WorkingHoursEnd = new TimeOnly(17, 0),
            BreakTimeStart = new TimeOnly(8, 0),
            BreakTimeEnd = new TimeOnly(18, 0),
            UseSystemWorkingHours = false,
            UseSystemBreakTime = false
        };

        var service = new UserWorkingHoursService(context);

        // Act
        var result = await service.EnforceUserWorkingHoursRules(userProfile);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("BreakTimeRange", result.ValidationErrors.Keys);
        var errorMessage = result.ValidationErrors["BreakTimeRange"][0];
        Assert.Contains("must be within your working hours range", errorMessage);
    }

    [Fact]
    public async Task EnforceUserWorkingHoursRules_TotalWorkingHoursLessThan7_ReturnsValidationFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        var userProfile = new UserProfile
        {
            FirstName = "Test",
            LastName = "User",
            WorkingHoursStart = new TimeOnly(9, 0),
            WorkingHoursEnd = new TimeOnly(15, 0), // 6 hours total
            BreakTimeStart = new TimeOnly(12, 0),
            BreakTimeEnd = new TimeOnly(12, 30), // 30 min break, net 5.5 hours
            UseSystemWorkingHours = false,
            UseSystemBreakTime = false
        };

        var service = new UserWorkingHoursService(context);

        // Act
        var result = await service.EnforceUserWorkingHoursRules(userProfile);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("TotalWorkingHours", result.ValidationErrors.Keys);
        var errorMessage = result.ValidationErrors["TotalWorkingHours"][0];
        Assert.Contains("Total shift time must be at least 7 hours", errorMessage);
    }
    
    [Fact]
    public async Task EnforceUserWorkingHoursRules_PartialSystemOverride_AppliesCorrectly()
    {
        await using var context = CreateInMemoryContext();
        context.Settings.Add(new Settings
        {
            Version = 1,
            DefaultWorkingHoursStart = new TimeSpan(8, 0, 0),
            DefaultWorkingHoursEnd = new TimeSpan(16, 0, 0),
            DefaultBreakTimeStart = new TimeSpan(11, 0, 0),
            DefaultBreakTimeEnd = new TimeSpan(12, 0, 0),
            DefaultUserRole = Shared.Enums.RoleName.User
        });
        await context.SaveChangesAsync();

        var userProfile = new UserProfile
        {
            FirstName = "Test",
            LastName = "User",
            WorkingHoursStart = new TimeOnly(9, 0),
            WorkingHoursEnd = new TimeOnly(17, 0),
            BreakTimeStart = new TimeOnly(12, 0),
            BreakTimeEnd = new TimeOnly(13, 0),
            UseSystemWorkingHours = true,
            UseSystemBreakTime = false
        };

        var service = new UserWorkingHoursService(context);
        var result = await service.EnforceUserWorkingHoursRules(userProfile);

        Assert.True(result.IsSuccess);
        Assert.Equal(new TimeOnly(8, 0), userProfile.WorkingHoursStart);
        Assert.Equal(new TimeOnly(16, 0), userProfile.WorkingHoursEnd);
        Assert.Equal(new TimeOnly(12, 0), userProfile.BreakTimeStart); // Should remain as user set
        Assert.Equal(new TimeOnly(13, 0), userProfile.BreakTimeEnd);   // Should remain as user set
    }
}