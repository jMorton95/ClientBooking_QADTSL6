using ClientBooking.Data.Entities;
using ClientBooking.Shared.Extensions;

namespace ClientBooking.Features.Bookings;

public class BookingFormData
{
    public required Client Client { get; set; }
    public TimeSpan WorkingHoursStart { get; set; }
    public TimeSpan WorkingHoursEnd { get; set; }
    public TimeSpan BreakTimeStart { get; set; }
    public TimeSpan BreakTimeEnd { get; set; }
    public bool DoesWorkWeekends { get; set; }
    
    public static BookingFormData GetFormData(Client client, User user, Settings systemSettings)
    {
        var (workingHoursStart, workingHoursEnd, breakTimeStart, breakTimeEnd) = user.GetEffectiveWorkingHours(systemSettings);
        
        return new BookingFormData
        {
            Client = client,
            WorkingHoursStart = workingHoursStart,
            WorkingHoursEnd = workingHoursEnd,
            BreakTimeStart = breakTimeStart,
            BreakTimeEnd = breakTimeEnd,
            DoesWorkWeekends = user.DoesWorkWeekends
        };
    }
}