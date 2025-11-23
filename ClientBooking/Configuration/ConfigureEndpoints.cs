using ClientBooking.Features;
using ClientBooking.Features.Bookings.Cancel;
using ClientBooking.Features.Bookings.Create;
using ClientBooking.Features.Bookings.View;
using ClientBooking.Features.Clients.Create;
using ClientBooking.Features.Clients.Update;
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
        app.MapRequestHandlers<RegistrationHandler>()
            .MapRequestHandlers<LoginHandler>()
            .MapRequestHandlers<LogoutHandler>()
            .MapRequestHandlers<ToggleAdminHandler>()
            .MapRequestHandlers<UpdateSettingsHandler>()
            .MapRequestHandlers<UpdateUserHandler>()
            .MapRequestHandlers<GetClientsHandler>()
            .MapRequestHandlers<CreateClientHandler>()
            .MapRequestHandlers<UpdateClientHandler>()
            .MapRequestHandlers<CreateBookingHandler>()
            .MapRequestHandlers<BookingsHandler>()
            .MapRequestHandlers<CancelBookingHandler>();
            
        var api = app.MapGroup("/api/");
                
        api.MapRequestHandlers<HealthCheckHandler>();
    }
    
    private static IEndpointRouteBuilder MapRequestHandlers<TRequestHandler>
        (this IEndpointRouteBuilder app) where TRequestHandler : IRequestHandler
    {
        TRequestHandler.Map(app);
        return app;
    }
}