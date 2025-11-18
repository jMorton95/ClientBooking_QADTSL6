using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;

namespace ClientBooking.Shared.Models;

public class UserProfile(User user)
{
    public string FirstName => user.FirstName;
    public string LastName => user.LastName;
    public string Email => user.Email;
    public string FullName =>  $"{user.FirstName} {user.LastName}";
    public TimeSpan WorkingHoursStart => user.WorkingHoursStart;
    public TimeSpan WorkingHoursEnd => user.WorkingHoursEnd;
    public TimeSpan BreakTimeStart => user.BreakTimeStart;
    public TimeSpan BreakTimeEnd => user.BreakTimeEnd;
    public List<UserBooking> UserBookings => user.UserBookings.ToList();
    public List<UserUnavailability> UnavailabilityPeriods => user.UnavailabilityPeriods.ToList();
    public List<Notification> Notifications => user.Notifications.ToList();
}