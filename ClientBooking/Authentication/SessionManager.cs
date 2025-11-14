namespace ClientBooking.Authentication;

public interface ISessionManager
{
    int? GetUserId();
    void Clear();
}

public class SessionManager(IHttpContextAccessor httpContextAccessor) : ISessionManager
{
    private ISession Session => httpContextAccessor.HttpContext!.Session;

    public int? GetUserId()
    {
        return Session.GetInt32("UserId");
    }

    public void Clear()
    {
        Session.Clear();
    }
}