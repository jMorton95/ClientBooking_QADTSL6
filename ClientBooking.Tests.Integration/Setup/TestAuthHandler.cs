
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClientBooking.Tests.Integration.Setup;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
{
    public const string Scheme = "TestAuth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Fake user for testing
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
