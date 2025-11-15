namespace ClientBooking.Features;

public class HtmxRedirectResult(string redirectUrl) : IResult
{
    //Custom IResult response that interacts with client-side library HTMX.
    //This sets Response Headers that tell HTMX to redirect to a specified URL.
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.Append("HX-Redirect", redirectUrl);
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        await httpContext.Response.WriteAsync(string.Empty);
    }
}