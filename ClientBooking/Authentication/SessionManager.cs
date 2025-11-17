namespace ClientBooking.Authentication;

public interface ISessionManager
{
    int? GetUserId();
    void SetUserId(int userId);
    void Logout();
    bool IsAuthenticated();
}

public class SessionManager(IHttpContextAccessor httpContextAccessor) : ISessionManager
{
    private ISession Session => httpContextAccessor.HttpContext!.Session;

    public int? GetUserId() => Session.GetInt32("UserId");
    
    public void SetUserId(int userId) => Session.SetInt32("UserId", userId);

    public void Logout() => Session.Clear();
    
    public bool IsAuthenticated() => GetUserId() != null;
}