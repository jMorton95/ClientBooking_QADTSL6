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

        // Only validate that custom hours are provided and valid when overriding system defaults
        When(x => !x.UseSystemWorkingHours, () =>
        {
            RuleFor(x => x.WorkingHoursStart)
                .NotNull().WithMessage("Working hours start is required when overriding system defaults.")
                .LessThan(x => x.WorkingHoursEnd)
                .WithMessage("Working hours start must be before end.");

            RuleFor(x => x.WorkingHoursEnd)
                .NotNull().WithMessage("Working hours end is required when overriding system defaults.");
        });

        When(x => !x.UseSystemBreakTime, () =>
        {
            RuleFor(x => x.BreakTimeStart)
                .NotNull().WithMessage("Break time start is required when overriding system defaults.")
                .LessThan(x => x.BreakTimeEnd)
                .WithMessage("Break time start must be before end.");

            RuleFor(x => x.BreakTimeEnd)
                .NotNull().WithMessage("Break time end is required when overriding system defaults.");
        });
    }
}