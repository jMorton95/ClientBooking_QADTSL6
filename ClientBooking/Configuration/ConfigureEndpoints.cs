using ClientBooking.Features;
using ClientBooking.Features.HealthCheck;
using ClientBooking.Features.Login;
using ClientBooking.Features.Logout;
using ClientBooking.Features.Registration;
using ClientBooking.Features.ToggleAdmin;
using ClientBooking.Features.UpdateSettings;

namespace ClientBooking.Configuration;

public static class ConfigureEndpoints
{
    public static void MapApplicationRequestHandlers(this WebApplication app)
    {
        app.MapRequestHandler<RegistrationHandler>()
            .MapRequestHandler<LoginHandler>()
            .MapRequestHandler<LogoutHandler>()
            .MapRequestHandler<ToggleAdminHandler>()
            .MapRequestHandler<UpdateSettingsHandler>();
            
        var api = app.MapGroup("/api/");
                
        api.MapRequestHandler<HealthCheckHandler>();
    }
    
    private static IEndpointRouteBuilder MapRequestHandler<TRequestHandler>
        (this IEndpointRouteBuilder app) where TRequestHandler : IRequestHandler
    {
        TRequestHandler.Map(app);
        return app;
    }
}