using ClientBooking.Data.Entities;

namespace ClientBooking.Shared.Extensions;

public static class UserExtensions
{
    //Extension that resolves schedule / break times, ensuring they are populated by personal overrides, or system defaults.
    public static (TimeSpan start, TimeSpan end, TimeSpan breakStart, TimeSpan breakEnd) 
        GetEffectiveWorkingHours(this User user, Settings systemSettings)
    {
        return (
            user.WorkingHoursStart ?? systemSettings.DefaultWorkingHoursStart,
            user.WorkingHoursEnd ?? systemSettings.DefaultWorkingHoursEnd,
            user.BreakTimeStart ?? systemSettings.DefaultBreakTimeStart,
            user.BreakTimeEnd ?? systemSettings.DefaultBreakTimeEnd
        );
    }
}