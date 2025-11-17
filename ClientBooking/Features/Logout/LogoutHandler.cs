using ClientBooking.Authentication;

namespace ClientBooking.Features.Logout;

public class LogoutHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", Handle);
    }

    private static HtmxRedirectResult Handle(ISessionManager sessionManager)
    {
        sessionManager.Logout();

        return new HtmxRedirectResult("/login");
    }
}