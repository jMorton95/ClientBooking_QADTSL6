using ClientBooking.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClientBooking.Tests.Integration.Setup;


public class IntegrationTestApplicationFactory : WebApplicationFactory<global::Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices((_, services) =>
        {
            var descriptorType =
                typeof(DbContextOptions<DataContext>);

            var descriptor = services
                .SingleOrDefault(s => s.ServiceType == descriptorType);

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }
            
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                    options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.Scheme, options => { });
            
            services.AddDbContext<DataContext>(options => 
                options.UseNpgsql(SharedContainerFixture.DatabaseContainer?.GetConnectionString()));
            
        });

        return base.CreateHost(builder);
    }
}