using ClientBooking.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClientBooking.Tests.Integration.Setup;

public class IntegrationTestApplicationFactory : WebApplicationFactory<global::Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid():N}";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        
        builder.ConfigureServices((context, services) =>
        {
            // Remove existing DataContext
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DataContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Create new test DB in the container
            var connectionString = SharedContainerFixture.DatabaseContainer.GetConnectionString();
            var builderConnection = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = _databaseName
            };

            // Add DbContext pointing to new database
            services.AddDbContext<DataContext>(options =>
                options.UseNpgsql(builderConnection.ConnectionString));
            
            services.AddAntiforgery(options =>
            {
                // This will accept any token for testing
                options.SuppressXFrameOptionsHeader = true;
                options.HeaderName = "X-CSRF-TOKEN";
            });
        });

        var host = base.CreateHost(builder);

        // Ensure database exists and migrations are applied
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        db.Database.EnsureCreated();
        db.Database.Migrate();

        return host;
    }
}