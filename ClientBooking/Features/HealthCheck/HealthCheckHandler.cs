using ClientBooking.Authentication;
using ClientBooking.Data;

namespace ClientBooking.Features.Health;

public class HealthCheckHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/healthcheck", HandleHealthCheck);
    }

    private static async Task<Results<Ok<HealthCheckResponse>, InternalServerError<string>>> 
        HandleHealthCheck(ISessionManager sessionManager, DataContext dataContext)
    {
        try
        {
            var canConnectToDatabase = await dataContext.Database.CanConnectAsync();
            var userSessionId = sessionManager.GetUserId();
            
            return TypedResults.Ok(new HealthCheckResponse(canConnectToDatabase, userSessionId));
        }
        catch (Exception ex)
        {
            return TypedResults.InternalServerError(ex.Message);
        }
    }
    
    private record HealthCheckResponse(bool DatabaseHealthy, int? UserSessionId);
}