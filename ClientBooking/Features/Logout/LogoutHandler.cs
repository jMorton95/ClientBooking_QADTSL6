using ClientBooking.Authentication;

namespace ClientBooking.Features.Logout;

public class LogoutHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", Handle);
    }

    private static HtmxRedirectResult Handle(ISessionStateManager sessionManager)
    {
        sessionManager.LogoutAsync();

        return new HtmxRedirectResult("/login");
    }
}