namespace ClientBooking.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) { }

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
}