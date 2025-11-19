using ClientBooking.Data.Entities;
using ClientBooking.Features.UpdateSettings;
using ClientBooking.Shared.TimeConversions;

namespace ClientBooking.Shared.Mapping;

public static class SettingsMapping
{
    public static UpdateSettingsRequest ToUpdateSettingsRequest(this Settings settings)
    {
        return new UpdateSettingsRequest
        {
            DefaultWorkingHoursStart = settings.DefaultWorkingHoursStart.ToTimeOnly(),
            DefaultWorkingHoursEnd = settings.DefaultWorkingHoursEnd.ToTimeOnly(),
            DefaultBreakTimeStart = settings.DefaultBreakTimeStart.ToTimeOnly(),
            DefaultBreakTimeEnd = settings.DefaultBreakTimeEnd.ToTimeOnly(),
            DefaultBookingDuration = settings.DefaultBookingDuration,
            DefaultUserRole = settings.DefaultUserRole,
            Version = settings.Version
        };
    }

    public static Settings ToSettingsEntity(this UpdateSettingsRequest request)
    {
        return new Settings
        {
            DefaultWorkingHoursStart = request.DefaultWorkingHoursStart.ToTimeSpan(),
            DefaultWorkingHoursEnd = request.DefaultWorkingHoursEnd.ToTimeSpan(),
            DefaultBreakTimeStart = request.DefaultBreakTimeStart.ToTimeSpan(),
            DefaultBreakTimeEnd = request.DefaultBreakTimeEnd.ToTimeSpan(),
            DefaultBookingDuration = request.DefaultBookingDuration,
            DefaultUserRole = request.DefaultUserRole
        };
    }
}