namespace ClientBooking.Data;

public static class RuntimeMigrator
{
    /// <summary>
    /// Checks if any Database migrations need to be applied to the runtime environment and applies them if detected.
    /// Check if Database settings have been previously seeded, if not, seeds the database.
    /// </summary>
    /// <param name="app">IApplicationBuilder</param>
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