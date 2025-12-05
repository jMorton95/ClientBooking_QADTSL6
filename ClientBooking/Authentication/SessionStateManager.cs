using System.ComponentModel;
using System.Security.Claims;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Shared.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ClientBooking.Authentication;

public interface ISessionStateManager
{
    int? GetUserSessionId();
    Task LoginAsync(User user, bool persistSession = false);
    Task LogoutAsync();
    bool IsAuthenticated();
    bool IsUserSessionAdministrator();
    Task RefreshUserSession(DataContext dataContext);
}

public class SessionStateManager(IHttpContextAccessor httpContextAccessor, ILogger<SessionStateManager> logger) : ISessionStateManager
{
    private HttpContext HttpContext => httpContextAccessor.HttpContext!;

    //Hook into HttpContext to access a User ID claim.
    public int? GetUserSessionId()
    {
        if (HttpContext?.User is null) return null;
        
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (int.TryParse(userIdClaim, out var userId))
            return userId;

        return null;
    }

    //With a userId, create a cookie-based session with the active user
    public async Task LoginAsync(User user, bool persistSession = false)
    {
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, user.Id.ToString())];
        
        //Store roles in session state.
        foreach (var userRole in user.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name.ToString()));
        }
        
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        
        //Determines whether a session expires when the user closes the browser, or is persisted for 3 hours (and refreshed with new requests)
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = persistSession,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = persistSession ? DateTimeOffset.UtcNow.Add(TimeSpan.FromHours(3)) : null,
        };
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        
        logger.LogInformation("User {UserEmail} logged in. Persistent Session: {PersistentSession}", user.Email, persistSession.ToString());
    }

    //Destroy the currently active session
    public async Task LogoutAsync()
    {
        var userId = GetUserSessionId();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        logger.LogInformation("User {userId} logged out.", userId);
    }

    //Determine whether a session is active or not.
    public bool IsAuthenticated() => GetUserSessionId() != null;

    public bool IsUserSessionAdministrator() => HasSpecificRole(RoleName.Admin);

    //Helper method to determine whether a user has a specific role.
    private bool HasSpecificRole(RoleName roleName)
    {
        if (!Enum.IsDefined(roleName))
            throw new InvalidEnumArgumentException(nameof(roleName), (int)roleName, typeof(RoleName));
        
        var userId = GetUserSessionId();

        if (userId is null)
            return false;
        
        var hasSpecificRole = HttpContext.User.Claims.Any(
            c => c.Type == ClaimTypes.Role && c.Value == roleName.ToString());
        
        return hasSpecificRole;
    }
    
    //Refresh the user session by re-logging in with the same user details.
    public async Task RefreshUserSession(DataContext dataContext)
    {
        var userId = GetUserSessionId();
        var user = await dataContext.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            return;
        }
        
        await LoginAsync(user);
        
        logger.LogInformation("User {userId} session refreshed.", userId);
    }
}