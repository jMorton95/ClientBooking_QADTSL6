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
    
    private static async Task<RegistrationResult> Handler([FromForm] Request request,  IValidator<RegistrationRequest> validator,
        DataContext dataContext,
        IPasswordHelper passwordHelper)
    {
        try
        {
            //Ensure the Request Object matches our validation criteria
            var registrationRequest = request.RegistrationRequest;
            var validationResult = await validator.ValidateAsync(registrationRequest);

            if (!validationResult.IsValid)
                return RegistrationResult.Fail(validationResult.Errors.Select(x => x.ErrorMessage).ToArray());
        
            //Ensure an account for this email address doesn't already exist
            if (await dataContext.Users.AnyAsync(u => u.Email == registrationRequest.Email))
                return RegistrationResult.Fail("Email already exists.");

            //Hash the password and create the user in the database.
            var hashedPassword = passwordHelper.HashPassword(registrationRequest.PasswordTwo);
            var newUser = registrationRequest.MapRegistrationRequestToUser(hashedPassword);
        
            await dataContext.Users.AddAsync(newUser);
            await dataContext.SaveChangesAsync();

            return RegistrationResult.Success(newUser.Id);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return RegistrationResult.Fail(e.Message);
        }
    }
    
    public record Request(RegistrationRequest RegistrationRequest);
    
    //DTO used to communicate the result of the registration operation.
    public record RegistrationResult(bool Result, string[] ErrorMessages, int? UserId)
    {
        public static RegistrationResult Fail(params string[] errorMessages)
            => new (false, errorMessages, null);
        
        public static RegistrationResult Success(int userId) => new  (true, [], userId);
    }
}