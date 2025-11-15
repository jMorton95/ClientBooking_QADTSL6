namespace ClientBooking.Authentication;

public class AuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ISessionManager sessionManager)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() is not null
            || context.Request.Path.StartsWithSegments("/login")
            || context.Request.Path.StartsWithSegments("/register"))
        {
            await next(context);
            return;
        }

        if (sessionManager.IsAuthenticated())
        {
            context.Items["UserId"] = sessionManager.GetUserId();
        }
        else
        {
            context.Response.Redirect("/login");
        }
    }
}