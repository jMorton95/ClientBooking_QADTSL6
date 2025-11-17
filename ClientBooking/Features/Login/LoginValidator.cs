using FluentValidation;

namespace ClientBooking.Features.Login;

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty()
            .WithMessage("Please enter your email address.");
        
        RuleFor(r => r.Password)
            .NotEmpty()
            .WithMessage("Please enter your password.");
    }
}