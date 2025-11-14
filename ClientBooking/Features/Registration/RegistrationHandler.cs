using ClientBooking.Authentication;
using ClientBooking.Data;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Registration;

public class RegistrationHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("register", Handler);
    }
    
    private static async Task<Results<RedirectHttpResult, BadRequest<string>, ValidationProblem, InternalServerError<string>>> Handler([FromForm] Request request,  IValidator<RegistrationRequest> validator,
        DataContext dataContext,
        IPasswordHelper passwordHelper,
        ISessionManager sessionManager)
    {
        try
        {
            //Ensure the Request Object matches our validation criteria
            var registrationRequest = request.RegistrationRequest;
            var validationResult = await validator.ValidateAsync(registrationRequest);

            if (!validationResult.IsValid)
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
        
            //Ensure an account for this email address doesn't already exist
            if (await dataContext.Users.AnyAsync(u => u.Email == registrationRequest.Email))
                return TypedResults.BadRequest("An account with this email address already exists.");

            //Hash the password and create the user in the database.
            var hashedPassword = passwordHelper.HashPassword(registrationRequest.PasswordTwo);
            var newUser = registrationRequest.MapRegistrationRequestToUser(hashedPassword);
        
            await dataContext.Users.AddAsync(newUser);
            await dataContext.SaveChangesAsync();
            
            sessionManager.SetUserId(newUser.Id);

            return TypedResults.Redirect("/");
        }
        catch (Exception ex)
        {
            //TODO: Add Logging
            Console.WriteLine(ex);
            return TypedResults.InternalServerError(ex.Message);
        }
    }
    
    //Wrapper DTO to capture RegistrationRequest
    public record Request(RegistrationRequest RegistrationRequest);
}