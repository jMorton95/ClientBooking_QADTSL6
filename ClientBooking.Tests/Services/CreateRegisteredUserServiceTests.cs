using ClientBooking.Data.Entities;
using ClientBooking.Features.Registration;
using ClientBooking.Shared.Enums;
using ClientBooking.Shared.Services;
using ClientBooking.Tests.Setup;
using Microsoft.EntityFrameworkCore;

namespace ClientBooking.Tests.Services;

public class CreateRegisteredUserServiceTests : UnitTestContext
{
    [Fact]
    public async Task CreateUserWithDefaultSettings_UserCreatedWithDefaultRoleAndSettings()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        // Seed settings
        var settings = new Settings
        {
            Version = 1,
            DefaultWorkingHoursStart = new TimeSpan(9, 0, 0),
            DefaultWorkingHoursEnd = new TimeSpan(17, 0, 0),
            DefaultBreakTimeStart = new TimeSpan(12, 0, 0),
            DefaultBreakTimeEnd = new TimeSpan(13, 0, 0),
            DefaultUserRole = RoleName.User
        };
        context.Settings.Add(settings);
        await context.SaveChangesAsync();

        var registrationRequest = new RegistrationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };
        var hashedPassword = "hashedpassword";

        var service = new CreateRegisteredUserService(context);

        // Act
        var user = await service.CreateUserWithDefaultSettings(registrationRequest, hashedPassword);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal(hashedPassword, user.HashedPassword);
        Assert.NotEmpty(user.UserRoles);
        Assert.Contains(user.UserRoles, ur => ur.Role.Name == RoleName.User);

        // Ensure it was saved to the DB
        var userInDb = await context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(userInDb);
        Assert.Contains(userInDb.UserRoles, ur => ur.Role.Name == RoleName.User);
    }

    [Fact]
    public async Task CreateUserWithDefaultSettings_DefaultRoleDoesNotExist_CreatesRole()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        // Seed settings only
        var settings = new Settings
        {
            Version = 1,
            DefaultUserRole = RoleName.User,
            DefaultWorkingHoursStart = TimeSpan.Zero,
            DefaultWorkingHoursEnd = TimeSpan.Zero,
            DefaultBreakTimeStart = TimeSpan.Zero,
            DefaultBreakTimeEnd = TimeSpan.Zero
        };
        context.Settings.Add(settings);
        await context.SaveChangesAsync();

        var registrationRequest = new RegistrationRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com"
        };
        var hashedPassword = "hashedpassword";

        var service = new CreateRegisteredUserService(context);

        // Act
        var user = await service.CreateUserWithDefaultSettings(registrationRequest, hashedPassword);

        // Assert
        Assert.NotNull(user);
        Assert.NotEmpty(user.UserRoles);
        Assert.Contains(user.UserRoles, ur => ur.Role.Name == RoleName.User);

        // Ensure role was added to DB
        var roleInDb = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleName.User);
        Assert.NotNull(roleInDb);
    }

    [Fact]
    public async Task CreateUserWithDefaultSettings_MultipleUsers_AssignsSameDefaultRole()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        // Seed default role manually
        var defaultRole = new Role { Name = RoleName.User };
        context.Roles.Add(defaultRole);

        // Seed settings
        var settings = new Settings
        {
            Version = 1,
            DefaultUserRole = RoleName.User,
            DefaultWorkingHoursStart = TimeSpan.Zero,
            DefaultWorkingHoursEnd = TimeSpan.Zero,
            DefaultBreakTimeStart = TimeSpan.Zero,
            DefaultBreakTimeEnd = TimeSpan.Zero
        };
        context.Settings.Add(settings);
        await context.SaveChangesAsync();

        var service = new CreateRegisteredUserService(context);

        var user1 = new RegistrationRequest { FirstName = "User1", LastName = "Test", Email = "u1@test.com" };
        var user2 = new RegistrationRequest { FirstName = "User2", LastName = "Test", Email = "u2@test.com" };

        // Act
        var createdUser1 = await service.CreateUserWithDefaultSettings(user1, "pass1");
        var createdUser2 = await service.CreateUserWithDefaultSettings(user2, "pass2");

        // Assert
        var roleId1 = createdUser1.UserRoles.First().RoleId;
        var roleId2 = createdUser2.UserRoles.First().RoleId;
        Assert.Equal(roleId1, roleId2);
    }
}