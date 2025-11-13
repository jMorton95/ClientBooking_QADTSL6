using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;

namespace ClientBooking.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<Settings> Settings { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserUnavailability> UserUnavailabilities { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    
    public DbSet<UserBooking> UserBookings { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    
    public override Task<int> SaveChangesAsync(CancellationToken ct = new())
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not Entity entity)
                continue;
            
            entity.SavedAt = DateTime.UtcNow;
            entity.RowVersion = entity.RowVersion >= 1 ? entity.RowVersion + 1 : 1;
        }

        return base.SaveChangesAsync(ct);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //Composite Keys
        modelBuilder.Entity<UserBooking>().HasKey(ub => new { ub.BookingId, ub.UserId });
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });

        //Use RowVersion has concurrency provider
        modelBuilder.Entity<Entity>()
            .Property(e => e.RowVersion)
            .IsConcurrencyToken();
        
        //Entity relationships
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Client)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserBooking>()
            .HasOne(ub => ub.Booking)
            .WithMany(b => b.UserBookings)
            .HasForeignKey(ub => ub.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserBooking>()
            .HasOne(ub => ub.User)
            .WithMany(u => u.UserBookings)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserUnavailability>()
            .HasOne(u => u.User)
            .WithMany(x => x.UnavailabilityPeriods)
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Booking)
            .WithMany(b => b.Notifications)
            .HasForeignKey(n => n.BookingId)
            .OnDelete(DeleteBehavior.NoAction);
        
        
        modelBuilder.Entity<Entity>()
            .HasOne(e => e.SavedBy)
            .WithMany()
            .HasForeignKey(e => e.SavedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}