using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Features.Registration;
using FluentValidation;

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

        public void AddCustomAuthenticationServices()
        {
            builder.Services
                .AddScoped<ISessionManager, SessionManager>();
                
            builder.Services
                .AddTransient<IPasswordHelper, PasswordHelper>()
                .AddTransient<IPasswordHasher, PasswordHasher>();
        }

        public void AddCustomValidators()
        {
            builder.Services
                .AddScoped<IValidator<RegistrationRequest>, RegistrationValidator>();
        }

        public void AddCustomRequestHandlers()
        {
            builder.Services.AddScoped<RegistrationHandler>();
        }
    }
}