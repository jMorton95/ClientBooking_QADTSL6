using System.Security.Claims;
using ClientBooking.Data;
using ClientBooking.Data.Entities;

namespace ClientBooking.Authentication;

public interface IAuthenticationService
{
    Task<User?> ValidateUserAsync(string email, string password);
    Task LoginAsync(HttpContext httpContext, User user);
    Task LogoutAsync(HttpContext httpContext);
}

public class AuthenticationService(DataContext database, IPasswordHelper passwordHelper) : IAuthenticationService
{
    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        var userAccount = await database.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (userAccount is null)
        {
            return null;
        }

        return passwordHelper.CheckPassword(password, userAccount.HashedPassword)
            ? userAccount
            : null;
    }

    public async Task LoginAsync(HttpContext httpContext, User user)
    {
        httpContext.Session.SetInt32("UserId", user.Id);
        await Task.CompletedTask;
    }

    public async Task LogoutAsync(HttpContext httpContext)
    {
        httpContext.Session.Remove("UserId");
        await Task.CompletedTask;
    }
}