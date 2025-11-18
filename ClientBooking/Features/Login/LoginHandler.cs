using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Login;

public class LoginHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/login", HandleAsync).AllowAnonymous();
    }
    
    //Constants to define account lockout behaviour.
    //TODO: Make this configurable by system parameter, with environmental defaults
    private const int MaxFailedAttempts = 3;
    private static readonly DateTime LockoutDuration = DateTime.UtcNow.AddHours(1);

    private static async Task<Results<HtmxRedirectResult, RazorComponentResult<LoginPage>, BadRequest<string>>> HandleAsync(
        [FromForm] Request request,
        IValidator<LoginRequest> validator,
        IPasswordHelper passwordHelper,
        DataContext dataContext,
        ISessionStateManager sessionManager)
    {
        try
        {
            //Validate the request
            var validationResult = await validator.ValidateAsync(request.LoginRequest);
            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<LoginPage>(new
                {
                    request.LoginRequest,
                    ValidationErrors = validationResult.ToDictionary()
                });
            }

            //Authenticate and handle the success/failure side effects
            var (user, error) = await ValidateCredentialsAsync(
                request.LoginRequest.Email, 
                request.LoginRequest.Password, 
                passwordHelper, 
                dataContext);

            //Inform User of authentication failure.
            if (error is not null)
            {
                return new RazorComponentResult<LoginPage>(new
                {
                    request.LoginRequest,
                    ErrorMessage = error
                });
            }

            
            //Create the user session and redirect them to the home page.
            await sessionManager.LoginAsync(user!.Id, persistSession: request.LoginRequest.RememberMe);
            return new HtmxRedirectResult("/");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return TypedResults.BadRequest(ex.Message);
        }
    }
    
    private record Request(LoginRequest LoginRequest);
    
    private static async Task<(User? user, string? error)> ValidateCredentialsAsync(
        string email, 
        string password, 
        IPasswordHelper passwordHelper, 
        DataContext dataContext)
    {
        var user = await dataContext.Users.SingleOrDefaultAsync(u => u.Email == email);
        
        if (user is null)
        {
            return (null, "User not found.");
        }

        //If already locked out, return early to ensure successful attempts do not bypass timeout.
        if (user.IsLockedOut && user.LockoutEnd > DateTime.UtcNow)
        {
            return (user, $"Incorrect password, your account has been locked. You can try again at {user.LockoutEnd}");
        }

        //If password comparison fails, incur side effects.
        if (!passwordHelper.CheckPassword(password, user.HashedPassword))
        {
            await HandleFailedLoginAsync(user, dataContext);
            return (user, user.IsLockedOut 
                ? $"Incorrect password, your account has been locked. You can try again at {user.LockoutEnd}"
                : "Incorrect password.");
        }

        //Otherwise, succesfully log the user in.
        await HandleSuccessfulLoginAsync(user, dataContext);
        return (user, null);
    }

    //Increased the fail counter, lock out the user if max failed attempts reached.
    private static async Task HandleFailedLoginAsync(User user, DataContext dataContext)
    {
        //Handles the case where a user was previously locked out and has begun to fail login attempts again.
        if (user.IsLockedOut && user.LockoutEnd < DateTime.UtcNow)
        {
            await ResetFailedLoginAttemptsAsync(user, dataContext);
        }
        
        user.AccessFailedCount += 1;
        
        if (user.AccessFailedCount >= MaxFailedAttempts)
        {
            user.IsLockedOut = true;
            user.LockoutEnd = LockoutDuration;
        }
    
        await dataContext.SaveChangesAsync();
    }

    //Reset account lock properties after a successful login.
    private static async Task HandleSuccessfulLoginAsync(User user, DataContext dataContext)
    {
        if (user.IsLockedOut || user.AccessFailedCount > 0)
        {
            await ResetFailedLoginAttemptsAsync(user, dataContext);
        }
    }

    private static async Task ResetFailedLoginAttemptsAsync(User user, DataContext dataContext)
    {
        user.IsLockedOut = false;
        user.AccessFailedCount = 0;
        await dataContext.SaveChangesAsync();
    }
}