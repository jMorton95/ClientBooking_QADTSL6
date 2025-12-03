using ClientBooking.Authentication;
using ClientBooking.Data;

namespace ClientBooking.Features.HealthCheck;

public class HealthCheckHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/healthcheck", HandleHealthCheck);
    }

    private static async Task<Results<Ok<HealthCheckResponse>, InternalServerError<string>>> 
        HandleHealthCheck(ISessionStateManager sessionManager, DataContext dataContext, ILogger<HealthCheckHandler> logger)
    {
        try
        {
            var canConnectToDatabase = await dataContext.Database.CanConnectAsync();
            var userSessionId = sessionManager.GetUserSessionId();
            
            return TypedResults.Ok(new HealthCheckResponse(canConnectToDatabase, userSessionId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during health check.");
            return TypedResults.InternalServerError(ex.Message);
        }
    }
    
    private record HealthCheckResponse(bool DatabaseHealthy, int? UserSessionId);
}