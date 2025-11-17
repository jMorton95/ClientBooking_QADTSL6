using ClientBooking.Authentication;
using ClientBooking.Data;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Login;

public class LoginHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/login", HandleAsync).AllowAnonymous();
    }

    private static async Task<Results<HtmxRedirectResult, RazorComponentResult<LoginPage>, BadRequest<string>>> HandleAsync(
        [FromForm] Request request,
        IValidator<LoginRequest> validator,
        IPasswordHelper passwordHelper,
        DataContext dataContext,
        ISessionStateManager sessionManager)
    {
        try
        {
            //Ensure the Request Object matches our validation criteria
            var loginRequest = request.LoginRequest;
            var validationResult = await validator.ValidateAsync(loginRequest);
            
            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<LoginPage>(new
                {
                    loginRequest,
                    ValidationErrors = validationResult.ToDictionary()
                });
            }
            
            //Search the database for a User with the requested email address.
            var user = await dataContext.Users.SingleOrDefaultAsync(u => u.Email == loginRequest.Email);
            if (user is null)
            {
                return new RazorComponentResult<LoginPage>(new
                {
                    loginRequest,
                    ErrorMessage = "User not found."
                });
            }
            
            //Early return if the user is locked out. This stops them from refreshing the lockout time, even if they enter a correct password.
            if (user.IsLockedOut)
            {
                return new RazorComponentResult<LoginPage>(new
                {
                    loginRequest,
                    ErrorMessage = $"Incorrect password, your account has been locked. You can try again at {user.LockoutEnd}"
                });
            }

            //Compare DB password with request password with hash comparison
            //If failed increase the AccessFailedCount, and lock their account if necessary.
            if (!passwordHelper.CheckPassword(loginRequest.Password, user.HashedPassword))
            {
                user.AccessFailedCount += 1;

                if (user.AccessFailedCount >= 3)
                {
                    user.IsLockedOut = true;
                    user.LockoutEnd = DateTime.UtcNow.AddHours(1);
                }
                
                await dataContext.SaveChangesAsync();

                if (user.IsLockedOut)
                {
                    return new RazorComponentResult<LoginPage>(new
                    {
                        loginRequest,
                        ErrorMessage = $"Incorrect password, your account has been locked. You can try again at {user.LockoutEnd}"
                    });
                }
               
                return new RazorComponentResult<LoginPage>(new
                {
                    loginRequest,
                    ErrorMessage = "Incorrect password."
                });
            }

            if (user.IsLockedOut && user.LockoutEnd > DateTime.UtcNow)
            {
                user.IsLockedOut = false;
            }

            if (user.AccessFailedCount > 0)
            {
                user.AccessFailedCount = 0;
            }

            if (dataContext.ChangeTracker.HasChanges())
            {
                await dataContext.SaveChangesAsync();
            }
            
            //Store the user ID in the current session and redirect to the home page.
            await sessionManager.LoginAsync(user.Id, persistSession: loginRequest.RememberMe);
            return new HtmxRedirectResult("/");

        }
        catch (Exception ex)
        {
            //TODO: Add logging
            Console.WriteLine(ex.Message);
            return TypedResults.BadRequest(ex.Message);
        }
    }
    
    private record Request(LoginRequest LoginRequest);
}