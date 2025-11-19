using ClientBooking.Shared.Models;
using FluentValidation;

namespace ClientBooking.Features.Me.UpdateUser;

public class UserProfileValidator : AbstractValidator<UserProfile>
{
    public UserProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

        RuleFor(x => x.WorkingHoursStart)
            .LessThan(x => x.WorkingHoursEnd)
            .WithMessage("Working hours start must be before end.");

        RuleFor(x => x.BreakTimeStart)
            .LessThan(x => x.BreakTimeEnd)
            .WithMessage("Break time start must be before end.");
    }
}