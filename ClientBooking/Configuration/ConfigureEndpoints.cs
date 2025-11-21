using ClientBooking.Features;
using ClientBooking.Features.Clients;
using ClientBooking.Features.Clients.Create;
using ClientBooking.Features.Clients.View;
using ClientBooking.Features.HealthCheck;
using ClientBooking.Features.Login;
using ClientBooking.Features.Logout;
using ClientBooking.Features.Me.UpdateUser;
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
            .MapRequestHandler<UpdateSettingsHandler>()
            .MapRequestHandler<UpdateUserHandler>()
            .MapRequestHandler<GetClientsHandler>()
            .MapRequestHandler<CreateClientHandler>();
            
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