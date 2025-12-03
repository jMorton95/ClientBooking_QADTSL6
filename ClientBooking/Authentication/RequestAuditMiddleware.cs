namespace ClientBooking.Authentication;

public class RequestAuditMiddleware(
    RequestDelegate next,
    ILogger<RequestAuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ISessionStateManager sessionManager)
    {
        
        //Only log GET requests that do not request a file extension.
        if (context.Request.Method != HttpMethods.Get || Path.HasExtension(context.Request.Path.Value))
        {
            await next(context);
            return;
        }
        
        var user = sessionManager.GetUserSessionId();
        var url = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
    
        logger.LogInformation("User: {User}, URL: {URL}", user, url);
        
        await next(context);
    }
}