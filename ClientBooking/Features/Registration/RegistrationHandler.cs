using ClientBooking.Authentication;
using ClientBooking.Data;
using FluentValidation;

namespace ClientBooking.Features.Registration;


public class RegistrationHandler(IValidator<RegistrationRequest> validator, DataContext dataContext, IPasswordHelper passwordHelper)
{
    public async Task<RegistrationResult> HandleAsync(RegistrationRequest request)
    {
        //Ensure the Request Object matches our validation criteria
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
            return RegistrationResult.Fail(validationResult.Errors.Select(x => x.ErrorMessage).ToArray());
        
        //Ensure an account for this email address doesn't already exist
        if (await dataContext.Users.AnyAsync(u => u.Email == request.Email))
            return RegistrationResult.Fail("Email already exists.");

        //Hash the password and create the user in the database.
        var hashedPassword = passwordHelper.HashPassword(request.PasswordTwo);
        var newUser = request.MapRegistrationRequestToUser(hashedPassword);
        
        await dataContext.Users.AddAsync(newUser);
        await dataContext.SaveChangesAsync();

        return RegistrationResult.Success(newUser.Id);
    }

    
    //DTO used to communicate the result of the registration operation.
    public record RegistrationResult(bool Result, string[] ErrorMessages, int? UserId)
    {
        public static RegistrationResult Fail(params string[] errorMessages)
            => new (false, errorMessages, null);
        
        public static RegistrationResult Success(int userId) => new  (true, [], userId);
    }
}