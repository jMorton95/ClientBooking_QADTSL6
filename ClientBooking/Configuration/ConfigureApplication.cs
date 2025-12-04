using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Features.Bookings;
using ClientBooking.Features.Clients;
using ClientBooking.Features.Login;
using ClientBooking.Features.Me;
using ClientBooking.Features.Me.UpdateUser;
using ClientBooking.Features.Registration;
using ClientBooking.Features.UpdateSettings;
using ClientBooking.Shared.Models;
using ClientBooking.Shared.Services;
using FluentValidation;

namespace ClientBooking.Configuration;

public static class ConfigureApplication
{
    extension(WebApplicationBuilder builder)
    {
        //Pull database settings from environment configuration and register our data context with the PG provider
        public void AddPostgresDatabaseFromConfiguration()
        {
            var dbSettings = builder.Configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>();
            
            var connectionString =
                $"Host={dbSettings?.Host};Port={dbSettings?.Port};Database={dbSettings?.Database};Username={dbSettings?.Username};Password={dbSettings?.Password};Include Error Detail=true";
            
            builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(connectionString));
        }
        
        //Add business logic
        public void AddCustomAuthenticationServices()
        {
            builder.Services
                .AddScoped<ISessionStateManager, SessionStateManager>()
                .AddScoped<ICreateRegisteredUserService, CreateRegisteredUserService>()
                .AddScoped<IBookingService, BookingService>()
                .AddScoped<IUserWorkingHoursService, UserWorkingHoursService>();
                
            builder.Services
                .AddTransient<IPasswordHelper, PasswordHelper>()
                .AddTransient<IPasswordHasher, PasswordHasher>();
        }

        //Add validation logic
        public void AddCustomValidators()
        {
            builder.Services
                .AddScoped<IValidator<RegistrationRequest>, RegistrationValidator>()
                .AddScoped<IValidator<LoginRequest>, LoginValidator>()
                .AddScoped<IValidator<UpdateSettingsRequest>, UpdateSettingsValidator>()
                .AddScoped<IValidator<UserProfile>, UserProfileValidator>()
                .AddScoped<IValidator<ClientRequest>, ClientValidator>()
                .AddScoped<IValidator<BookingRequest>, BookingValidator>();
        }
    }
}