using ClientBooking.Authentication;

namespace ClientBooking.Features.Logout;

public class LogoutHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", Handle);
    }

    //Request handler that logs the user out by wiping their session state.
    public static HtmxRedirectResult Handle(ISessionStateManager sessionManager)
    {
        sessionManager.LogoutAsync();

        return new HtmxRedirectResult("/login");
    }
}