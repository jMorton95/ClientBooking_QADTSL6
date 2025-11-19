using ClientBooking.Data.Entities;
using ClientBooking.Shared.Models;

namespace ClientBooking.Shared.Mapping;

public static class UserMapping
{
    public static void MapUserFromUpdateUserProfileRequest(this User user, UserProfile request)
    {
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.WorkingHoursStart = request.WorkingHoursStart.ToTimeSpan();
        user.WorkingHoursEnd = request.WorkingHoursEnd.ToTimeSpan();
        user.BreakTimeStart = request.BreakTimeStart.ToTimeSpan();
        user.BreakTimeEnd = request.BreakTimeEnd.ToTimeSpan();
        user.DoesWorkWeekends = request.DoesWorkWeekends;
    }
    
    public static UserProfile MapToUserProfile(this User user)
    {
        return new UserProfile
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            WorkingHoursStart = user.WorkingHoursStart.ToTimeOnly(),
            WorkingHoursEnd = user.WorkingHoursEnd.ToTimeOnly(),
            BreakTimeStart = user.BreakTimeStart.ToTimeOnly(),
            BreakTimeEnd = user.BreakTimeEnd.ToTimeOnly(),
            DoesWorkWeekends = user.DoesWorkWeekends,
            UserBookings = user.UserBookings.ToList(),
            UnavailabilityPeriods = user.UnavailabilityPeriods.ToList(),
            Notifications = user.Notifications.ToList()
        };
    }
}