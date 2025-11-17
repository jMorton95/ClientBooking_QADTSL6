using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ClientBooking.Authentication;

public interface ISessionStateManager
{
    int? GetUserSessionId();
    Task LoginAsync(int userId, bool persistSession = false);
    Task LogoutAsync();
    bool IsAuthenticated();
}

public class SessionStateManager(IHttpContextAccessor httpContextAccessor) : ISessionStateManager
{
    private HttpContext HttpContext => httpContextAccessor.HttpContext!;

    //Hook into HttpContext to access a User ID claim.
    public int? GetUserSessionId()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (int.TryParse(userIdClaim, out var userId))
            return userId;

        return null;
    }

    //With a userId, create a cookie-based session with the active user
    public async Task LoginAsync(int userId, bool persistSession = false)
    {
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, userId.ToString())];
        
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        //Determines whether a session expires when the user closes the browser, or is persisted for 3 hours (and refreshed with new requests)
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = persistSession,
            ExpiresUtc = persistSession ? DateTimeOffset.UtcNow.Add(TimeSpan.FromHours(3)) : null
        };
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
    }

    //Destroy the currently active session
    public async Task LogoutAsync() => await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    
    //Determine whether a session is active or not.
    public bool IsAuthenticated() => GetUserSessionId() != null;
}