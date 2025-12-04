using FluentValidation;

namespace ClientBooking.Features.UpdateSettings;

public class UpdateSettingsValidator : AbstractValidator<UpdateSettingsRequest>
{
    //Validation rules for updating settings
    //Working hours and break hours must be valid must be valid
    public UpdateSettingsValidator()
    {
        RuleFor(x => x.DefaultWorkingHoursStart)
            .LessThan(x => x.DefaultWorkingHoursEnd)
            .WithMessage("Working hours start must be before end");

        RuleFor(x => x.DefaultBreakTimeStart)
            .LessThan(x => x.DefaultBreakTimeEnd)
            .WithMessage("Break time start must be before end");
    }
}