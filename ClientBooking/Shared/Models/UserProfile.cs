using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;

namespace ClientBooking.Shared.Models;

public class UserProfile
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public TimeOnly WorkingHoursStart { get; set; }
    public TimeOnly WorkingHoursEnd { get; set; }
    public TimeOnly BreakTimeStart { get; set; }
    public TimeOnly BreakTimeEnd { get; set; }

    public bool DoesWorkWeekends { get; set; } 
    public List<UserBooking> UserBookings { get; set; } 
    public List<UserUnavailability> UnavailabilityPeriods { get; set; } 
    public List<Notification> Notifications { get; set; } 
}