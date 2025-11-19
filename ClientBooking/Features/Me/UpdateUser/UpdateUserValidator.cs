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

        //Only validate custom hours if user is overriding system defaults, assume system defaults are independently validated elsewhere
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
        
        RuleFor(x => x)
            .Must(HaveMinimumEightHourShift)
            .WithMessage("Total shift time must be at least 7 hours (excluding breaks).")
            .When(x => !x.UseSystemWorkingHours && !x.UseSystemBreakTime);
    }

    
    //Ensure shifts are at minimum 7 cumulative hours.
    private bool HaveMinimumEightHourShift(UserProfile request)
    {
        if (request is { UseSystemWorkingHours: true, UseSystemBreakTime: true })
        {
            return true;
        }
        
        var workingDuration = request.WorkingHoursEnd - request.WorkingHoursStart;
        var breakDuration = request.BreakTimeEnd - request.BreakTimeStart;
        var netWorkingHours = workingDuration - breakDuration;

        return netWorkingHours >= TimeSpan.FromHours(7);
    }
}