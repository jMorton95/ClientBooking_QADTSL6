namespace ClientBooking.Authentication;

public class RequireAuthenticationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var session = context.HttpContext.RequestServices.GetRequiredService<ISessionManager>();

        if (!session.IsAuthenticated())
        {
            return Results.Redirect("/login");
        }

        return await next(context);
    }
}