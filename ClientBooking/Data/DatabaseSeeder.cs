using ClientBooking.Authentication;
using ClientBooking.Configuration;
using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Shared.Enums;
using Microsoft.Extensions.Options;

namespace ClientBooking.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var configurationSettings = scope.ServiceProvider.GetRequiredService<IOptions<ConfigurationSettings>>().Value;
        var passwordHelper = services.GetRequiredService<IPasswordHelper>();

        var genericSeederPassword = passwordHelper.HashPassword(configurationSettings.SystemAccountPassword ?? "");
        
        var now = DateTime.UtcNow;
        
        var adminUser = new User
        {
            FirstName = "System",
            LastName = "Admin",
            Email = "admin@joshmorton.co.uk",
            HashedPassword = genericSeederPassword,
            IsActive = true,
            SavedAt = now,
            RowVersion = 1,
            SavedById = null
        };

        var standardUser = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            HashedPassword = genericSeederPassword,
            IsActive = true,
            SavedAt = now,
            RowVersion = 1,
            SavedById = null
        };

        db.Users.AddRange(adminUser, standardUser);
        await db.SaveChangesAsync();

       
        var adminRole = new Role
        {
            Name = RoleName.Admin
        };

        var standardRole = new Role
        {
            Name = RoleName.User
        };

        db.Roles.AddRange(adminRole, standardRole);
        await db.SaveChangesAsync();

      
        db.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        db.UserRoles.Add(new UserRole
        {
            UserId = standardUser.Id,
            RoleId = standardRole.Id
        });

        await db.SaveChangesAsync();

     
        var client = new Client
        {
            Name = "John Doe",
            Description = "Example initial seeded client",
            Email = "john.doe@gmail.com",
            SavedAt = now,
            RowVersion = 1,
            SavedById = adminUser.Id
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var booking = new Booking
        {
            ClientId = client.Id,
            Notes = "Initial test booking",
            StartDateTime = now.AddDays(1).AddHours(9),
            EndDateTime = now.AddDays(1).AddHours(10),
            Status = BookingStatus.Confirmed,
            IsRecurring = true,
            RecurrencePattern = BookingRecurrencePattern.Weekly,
            NumberOfRecurrences = 24,
            SavedAt = now,
            RowVersion = 1,
            SavedById = adminUser.Id
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        db.UserBookings.Add(new UserBooking
        {
            BookingId = booking.Id,
            UserId = standardUser.Id
        });

        await db.SaveChangesAsync();
        
        var settings = new Settings
        {
            DefaultWorkingHoursStart = new TimeSpan(9, 0, 0),
            DefaultWorkingHoursEnd = new TimeSpan(17, 0, 0),
            DefaultBreakTimeStart = new TimeSpan(12, 0, 0),
            DefaultBreakTimeEnd =  new TimeSpan(13, 0, 0),
            DefaultUserRole = RoleName.User,
            Version = 1,
            RowVersion = 1,
            SavedAt = now,
            SavedById = adminUser.Id
        };

        db.Settings.Add(settings);

        await db.SaveChangesAsync();
    }
}
