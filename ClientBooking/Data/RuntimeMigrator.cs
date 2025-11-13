namespace ClientBooking.Data;

public static class RuntimeMigrator
{
    public static async Task ApplyStartupDatabaseMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();


        if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
        {
            await dbContext.Database.MigrateAsync();
        }
    }    
}