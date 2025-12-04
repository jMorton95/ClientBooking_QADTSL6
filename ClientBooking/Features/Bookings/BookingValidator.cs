using ClientBooking.Shared.Enums;
using FluentValidation;

namespace ClientBooking.Features.Bookings;

public class BookingValidator: AbstractValidator<BookingRequest>
{
    //Validation rules for creating a booking
    //Start and end date and time must be valid and start and end on the same day
    //If the booking is recurring, the recurrence pattern and number of recurrences must be specified
    public BookingValidator()
    {
        RuleFor(x => x.StartDateTime)
            .NotEmpty().WithMessage("Start date and time is required.")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Start date cannot be in the past.");

        RuleFor(x => x.EndDateTime)
            .NotEmpty().WithMessage("End date and time is required.")
            .GreaterThan(x => x.StartDateTime).WithMessage("End time must be after start time.");
        
        RuleFor(x => x.StartDateTime.DayOfWeek)
            .Equal(x => x.EndDateTime.DayOfWeek).WithMessage("Booking must start and end on the same day.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");

        When(x => x.IsRecurring, () =>
        {
            RuleFor(x => x.RecurrencePattern)
                .NotEqual(BookingRecurrencePattern.None).WithMessage("Please select a recurrence pattern.");

            RuleFor(x => x.NumberOfRecurrences)
                .InclusiveBetween(1, 12).WithMessage("Number of recurrences must be between 1 and 12.");
        });
    }
}