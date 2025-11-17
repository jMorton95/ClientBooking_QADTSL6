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

    public int? GetUserSessionId()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (int.TryParse(userIdClaim, out var userId))
            return userId;

        return null;
    }

    public async Task LoginAsync(int userId, bool persistSession = false)
    {
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, userId.ToString())];
        
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = persistSession,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromHours(3))
        };
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
    }

    public async Task LogoutAsync() => await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    
    public bool IsAuthenticated() => GetUserSessionId() != null;
}