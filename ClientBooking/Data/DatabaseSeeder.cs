using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;

namespace ClientBooking.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        var now = DateTime.UtcNow;
        
        var adminUser = new User
        {
            FirstName = "System",
            LastName = "Admin",
            Email = "admin@joshmorton.co.uk",
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
            IsActive = true,
            SavedAt = now,
            RowVersion = 1,
            SavedById = null
        };

        db.Users.AddRange(adminUser, standardUser);
        await db.SaveChangesAsync();

       
        var adminRole = new Role
        {
            Name = "Admin",
            SavedAt = now,
            RowVersion = 1,
            SavedById = adminUser.Id
        };

        var standardRole = new Role
        {
            Name = "Standard",
            SavedAt = now,
            RowVersion = 1,
            SavedById = adminUser.Id
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
            Name = "Default Client",
            Description = "Example initial seeded client",
            ClientWorkingHoursStart = new TimeSpan(9, 0, 0),
            ClientWorkingHoursEnd = new TimeSpan(17, 0, 0),
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
            Status = "Confirmed",
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
            CompanyWorkingHoursStart = new TimeSpan(9, 0, 0),
            CompanyWorkingHoursEnd = new TimeSpan(17, 0, 0),
            DefaultBookingDuration = 60,
            DefaultUserRole = "Standard",
            MaxDailyUserBookings = 5,
            AllowWeekendBookings = false,
            Version = 1,
            RowVersion = 1,
            SavedAt = now,
            SavedById = adminUser.Id
        };

        db.Settings.Add(settings);

        await db.SaveChangesAsync();
    }
}
