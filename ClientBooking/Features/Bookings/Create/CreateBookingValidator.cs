using ClientBooking.Shared.Enums;
using FluentValidation;

namespace ClientBooking.Features.Bookings.Create;

public class CreateBookingValidator : AbstractValidator<BookingRequest>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.ClientId)
            .GreaterThan(0).WithMessage("Please select a client.");

        RuleFor(x => x.StartDateTime)
            .NotEmpty().WithMessage("Start date and time is required.")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Start date cannot be in the past.");

        RuleFor(x => x.EndDateTime)
            .NotEmpty().WithMessage("End date and time is required.")
            .GreaterThan(x => x.StartDateTime).WithMessage("End time must be after start time.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");

        When(x => x.IsRecurring, () =>
        {
            RuleFor(x => x.RecurrencePattern)
                .NotEqual(BookingRecurrencePattern.None).WithMessage("Please select a recurrence pattern.");

            RuleFor(x => x.NumberOfRecurrences)
                .InclusiveBetween(1, 52).WithMessage("Number of recurrences must be between 1 and 52.");
        });
    }
}