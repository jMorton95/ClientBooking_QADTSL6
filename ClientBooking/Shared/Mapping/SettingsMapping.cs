using ClientBooking.Data.Entities;
using ClientBooking.Features.UpdateSettings;
using ClientBooking.Shared.Models;

namespace ClientBooking.Shared.Mapping;

//Mapper for Settings
//Used to convert Settings to UpdateSettingsRequest and vice versa
//TimeOnly overrides are used to convert TimeSpan to TimeOnly
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
            DefaultUserRole = request.DefaultUserRole
        };
    }
}