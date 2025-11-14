namespace ClientBooking.Authentication;

public class AuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ISessionManager sessionManager, ILogger<AuthenticationMiddleware> logger)
    {
        if (sessionManager.IsAuthenticated())
        {
            context.Items["UserId"] = sessionManager.GetUserId();
        }
        
        await next(context);
    }
}