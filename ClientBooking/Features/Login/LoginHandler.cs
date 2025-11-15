using ClientBooking.Authentication;
using ClientBooking.Data;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Login;

public class LoginHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/login", HandleAsync);
    }

    private static async Task<Results<HtmxRedirectResult, RazorComponentResult<LoginPage>, InternalServerError<string>>> HandleAsync(
        [FromForm] Request request,
        IValidator<LoginRequest> validator,
        IPasswordHelper passwordHelper,
        DataContext dataContext,
        ISessionManager sessionManager)
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

            //Compare DB password with request password with hash comparison
            if (!passwordHelper.CheckPassword(loginRequest.Password, user.HashedPassword))
            {
                return new RazorComponentResult<LoginPage>(new
                {
                    loginRequest,
                    ErrorMessage = "Incorrect password."
                });
            }
            
            //Store the user ID in the current session and redirect to the home page.
            sessionManager.SetUserId(user.Id);
            return new HtmxRedirectResult("/");

        }
        catch (Exception ex)
        {
            //TODO: Add logging
            Console.WriteLine(ex.Message);
            return TypedResults.InternalServerError(ex.Message);
        }
    }
    
    private record Request(LoginRequest LoginRequest);
}