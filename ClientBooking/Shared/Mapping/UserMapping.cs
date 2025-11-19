using ClientBooking.Data.Entities;
using ClientBooking.Shared.Extensions;
using ClientBooking.Shared.Models;

namespace ClientBooking.Shared.Mapping;

public static class UserMapping
{
    extension(User user)
    {
        //Mapper that interprets booleans from UserProfile to determine whether the User will save a custom work schedule / break time
        public void MapUserFromUpdateUserProfileRequest(UserProfile request)
        {
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.WorkingHoursStart = request is { UseSystemWorkingHours: false } ? request.WorkingHoursStart.ToTimeSpan() : null;
            user.WorkingHoursEnd = request is { UseSystemWorkingHours: false } ? request.WorkingHoursEnd.ToTimeSpan() : null;
            user.BreakTimeStart = request is { UseSystemBreakTime: false } ? request.BreakTimeStart.ToTimeSpan() : null;
            user.BreakTimeEnd = request is { UseSystemBreakTime: false } ? request.BreakTimeEnd.ToTimeSpan() : null;
            user.DoesWorkWeekends = request.DoesWorkWeekends;
            user.UseSystemWorkingHours = request.UseSystemWorkingHours;
            user.UseSystemBreakTime = request.UseSystemBreakTime;
        }

        //Mapper that ensures schedule / break times are populated by personal overrides, or system defaults.
        public UserProfile MapToUserProfile(Settings systemSettings)
        {
            var (effectiveStart, effectiveEnd, effectiveBreakStart, effectiveBreakEnd) = 
                user.GetEffectiveWorkingHours(systemSettings);
        
            return new UserProfile
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                WorkingHoursStart = user.WorkingHoursStart?.ToTimeOnly() ?? effectiveStart.ToTimeOnly(),
                WorkingHoursEnd = user.WorkingHoursEnd?.ToTimeOnly() ?? effectiveEnd.ToTimeOnly(),
                BreakTimeStart = user.BreakTimeStart?.ToTimeOnly() ?? effectiveBreakStart.ToTimeOnly(),
                BreakTimeEnd = user.BreakTimeEnd?.ToTimeOnly() ?? effectiveBreakEnd.ToTimeOnly(),
                DoesWorkWeekends = user.DoesWorkWeekends,
                UseSystemWorkingHours = user.UseSystemWorkingHours,
                UseSystemBreakTime = user.UseSystemBreakTime,
            };
        }
    }
}