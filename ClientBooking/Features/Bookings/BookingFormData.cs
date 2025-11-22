using ClientBooking.Data.Entities;

namespace ClientBooking.Features.Bookings;

public class BookingFormData
{
    public Client Client { get; set; }
    public TimeSpan WorkingHoursStart { get; set; }
    public TimeSpan WorkingHoursEnd { get; set; }
    public TimeSpan BreakTimeStart { get; set; }
    public TimeSpan BreakTimeEnd { get; set; }
    public bool DoesWorkWeekends { get; set; }
}