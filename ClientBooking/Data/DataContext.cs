using ClientBooking.Authentication;
using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;

namespace ClientBooking.Data;

public class DataContext(DbContextOptions<DataContext> options, ISessionStateManager sessionStateManager) : DbContext(options)
{
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserUnavailability> UserUnavailabilities => Set<UserUnavailability>();
    
    public DbSet<UserBooking> UserBookings => Set<UserBooking>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    
    public override async Task<int> SaveChangesAsync(CancellationToken ct = new())
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not Entity entity)
                continue;
            
            entity.SavedAt = DateTime.UtcNow;
            entity.SavedById = sessionStateManager.GetUserSessionId() ?? null;
            
            if (entry is { Entity: Settings settings, State: EntityState.Added })
            {
                var maxVersion = await Settings.MaxAsync(s => (int?)s.Version, ct) ?? 0;
                settings.Version = maxVersion + 1;
            }
            
            switch (entry.State)
            {
                case EntityState.Added:
                    entity.RowVersion = 1;
                    continue;
                case EntityState.Modified:
                    entity.RowVersion += 1;
                    continue;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                    default: continue;
            }
        }

        return await base.SaveChangesAsync(ct);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //Composite Keys
        modelBuilder.Entity<UserBooking>().HasKey(ub => new { ub.BookingId, ub.UserId });
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        
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
        
        modelBuilder.Entity<Settings>()
            .HasOne(s => s.SavedBy)
            .WithMany()
            .HasForeignKey(s => s.SavedById)
            .OnDelete(DeleteBehavior.NoAction);
        
        
        //Auto-Include navigation properties
        modelBuilder.Entity<User>()
            .Navigation(u => u.UserRoles)
            .AutoInclude();

        modelBuilder.Entity<UserRole>()
            .Navigation(ur => ur.Role)
            .AutoInclude();
    }
}