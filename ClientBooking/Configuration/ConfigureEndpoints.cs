using ClientBooking.Features;
using ClientBooking.Features.Health;
using ClientBooking.Features.Registration;

namespace ClientBooking.Configuration;

public static class ConfigureEndpoints
{
    public static void MapApplicationRequestHandlers(this WebApplication app)
    {
        app.MapRequestHandler<RegistrationHandler>()
            .MapRequestHandler<HealthCheckHandler>();
    }
    
    private static IEndpointRouteBuilder MapRequestHandler<TRequestHandler>
        (this IEndpointRouteBuilder app) where TRequestHandler : IRequestHandler
    {
        TRequestHandler.Map(app);
        return app;
    }
}