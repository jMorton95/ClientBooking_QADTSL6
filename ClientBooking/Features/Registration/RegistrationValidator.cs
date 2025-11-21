using FluentValidation;

namespace ClientBooking.Features.Registration;

public class RegistrationValidator : AbstractValidator<RegistrationRequest>
{
    public RegistrationValidator()
    {
        //FirstName + LastName + Email must follow the Maximum Length constraint applied to their database table
        RuleFor(x => x.FirstName)
            .MinimumLength(2)
            .MaximumLength(50);
        
        RuleFor(x => x.LastName)
            .MinimumLength(2)
            .MaximumLength(50);
        
        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(255);

        //Create a string validator that enforces password strength.
        var passwordRuleSet = new InlineValidator<string>();
        passwordRuleSet.RuleFor(x => x)
            .MinimumLength(12).WithMessage("Password must contain at least 12 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.PasswordOne).SetValidator(passwordRuleSet);
        RuleFor(x => x.PasswordTwo).SetValidator(passwordRuleSet);
        
        //Custom validation rule to ensure both passwords are matching.
        RuleFor(x => x.PasswordTwo)
            .Equal(x => x.PasswordOne)
            .WithMessage("Passwords must be matching.");
    }
}