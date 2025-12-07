using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Shared.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Registration;

public class RegistrationHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("register", Handler).AllowAnonymous();
    }
    
    //Request handler that registers a new user.
    //The registration request is validated and used to create the user entity.
    //The user is then logged in and redirected to the home page.
    public static async Task<Results<HtmxRedirectResult, RazorComponentResult<RegistrationPage>, InternalServerError<string>>> Handler(
        [FromForm] Request request, 
        IValidator<RegistrationRequest> validator,
        DataContext dataContext,
        IPasswordService passwordService,
        ISessionStateManager sessionManager,
        ICreateRegisteredUserService createRegisteredUserService,
        ILogger<RegistrationHandler> logger)
    {
        try
        {
            //Ensure the Request Object matches our validation criteria
            var registrationRequest = request.RegistrationRequest;
            var validationResult = await validator.ValidateAsync(registrationRequest);

            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<RegistrationPage>(new {
                    registrationRequest,
                    ValidationErrors = validationResult.ToDictionary()
                });
            }
        
            //Ensure an account for this email address doesn't already exist
            if (await dataContext.Users.AnyAsync(u => u.Email == registrationRequest.Email))
            {
                logger.LogError("An account with email address {email} already exists.", registrationRequest.Email);
                return new RazorComponentResult<RegistrationPage>(new {
                    registrationRequest,
                    ErrorMessage = "An account with this email address already exists."
                });
            }

            //Create the user in the database.
            var newUser = await createRegisteredUserService.CreateUserWithDefaultSettings(registrationRequest, registrationRequest.PasswordOne);
            
            logger.LogInformation("User {Email} successfully registered.", registrationRequest.Email);
            
            //Store the userId in the newly created session and inform HTMX to redirect.
            await sessionManager.LoginAsync(newUser, persistSession: true);
            return new HtmxRedirectResult("/");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while trying to register a new user.");
            return TypedResults.InternalServerError(ex.Message);
        }
    }
    
    //Wrapper DTO to capture RegistrationRequest
    public record Request(RegistrationRequest RegistrationRequest);
}