using ClientBooking.Authentication;
using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Shared.Enums;

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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    
    public override async Task<int> SaveChangesAsync(CancellationToken ct = new())
    {
        var auditLogsToAdd = new List<AuditLog>();
        
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not Entity entity)
                continue;
            
            entity.SavedAt = DateTime.UtcNow;
            entity.SavedById = sessionStateManager.GetUserSessionId() ?? null;
            
            if (entry is { Entity: Settings settings, State: EntityState.Added })
            {
                var lastVersion = await Settings.OrderByDescending(s => s.Version).LastOrDefaultAsync(ct);
                settings.Version = lastVersion?.Version + 1 ?? 1;
            }
            
            switch (entry.State)
            {
                case EntityState.Added:
                    entity.RowVersion = 1;
                    break;
                case EntityState.Modified:
                    entity.RowVersion += 1;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                    default: break;
            }
            
            var auditAction = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Modified => AuditAction.Update,
                _ => (AuditAction?)null
            };
            
            if (auditAction.HasValue)
            {
                auditLogsToAdd.Add(new AuditLog
                {
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = entity.Id.ToString(),
                    UserId = entity.SavedById,
                    Action = auditAction.Value,
                    Timestamp = DateTime.UtcNow,
                    UserName = await GetCurrentUserName(entity.SavedById, ct)
                });
            }
        }
        
        await AuditLogs.AddRangeAsync(auditLogsToAdd, ct);

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

        //Ensure default website settings are configured when first creating the database.
        modelBuilder.Entity<Settings>()
            .HasData([new Settings
                {
                    Id = 1,
                    DefaultWorkingHoursStart = new TimeSpan(9, 0, 0),
                    DefaultWorkingHoursEnd = new TimeSpan(17, 0, 0),
                    DefaultBreakTimeStart = new TimeSpan(12, 0, 0),
                    DefaultBreakTimeEnd = new TimeSpan(13, 0, 0),
                    DefaultUserRole = RoleName.User,
                    Version = 1,
                    RowVersion = 1,
                    SavedAt = new DateTime(2025, 11, 27, 15, 29, 5, DateTimeKind.Utc)
                }]);
        
        //Auto-Include navigation properties
        modelBuilder.Entity<User>()
            .Navigation(u => u.UserRoles)
            .AutoInclude();

        modelBuilder.Entity<UserRole>()
            .Navigation(ur => ur.Role)
            .AutoInclude();
    }
    
    private async Task<string?> GetCurrentUserName(int? userId, CancellationToken ct)
    {
        if (!userId.HasValue) return null;

        var user = await Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, ct);

        return user != null ? $"{user.FirstName} {user.LastName}" : null;
    }
}