using ClientBooking.Authentication;
using ClientBooking.Data;

namespace ClientBooking.Configuration;

public static class ConfigureApplication
{
    extension(WebApplicationBuilder builder)
    {
        public void AddPostgresDatabaseFromConfiguration()
        {
            var dbSettings = builder.Configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>();
            
            var connectionString =
                $"Host={dbSettings?.Host};Port={dbSettings?.Port};Database={dbSettings?.Database};Username={dbSettings?.Username};Password={dbSettings?.Password};Include Error Detail=true";
            
            builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(connectionString));
        }

        public void AddInjectableServices()
        {
            builder.Services
                .AddScoped<ISessionManager, SessionManager>();
                
            builder.Services
                .AddTransient<IPasswordHelper, PasswordHelper>()
                .AddTransient<IPasswordHasher, PasswordHasher>();
        }
    }
}