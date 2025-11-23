using ClientBooking.Shared.Enums;

namespace ClientBooking.Features.Bookings;

public class BookingRequest
{
    public string Notes { get; set; } = string.Empty;

    public DateTime StartDateTime { get; set => field = DateTime.SpecifyKind(value, DateTimeKind.Utc); }

    public DateTime EndDateTime { get; set => field = DateTime.SpecifyKind(value, DateTimeKind.Utc); }

    public bool IsRecurring { get; set; }
    public int NumberOfRecurrences { get; set; } = 2;
    public BookingRecurrencePattern RecurrencePattern { get; set; } = BookingRecurrencePattern.None;
}