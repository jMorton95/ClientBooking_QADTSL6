using ClientBooking.Shared.Enums;

namespace ClientBooking.Features.Bookings;

public class BookingRequest
{
    public string Notes { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsRecurring { get; set; }
    public int NumberOfRecurrences { get; set; }
    public BookingRecurrencePattern RecurrencePattern { get; set; } = BookingRecurrencePattern.None;
}