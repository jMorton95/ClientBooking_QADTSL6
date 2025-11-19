using ClientBooking.Shared.Enums;

namespace ClientBooking.Features.UpdateSettings;

public class UpdateSettingsRequest
{
    public TimeOnly DefaultWorkingHoursStart { get; set; }
    public TimeOnly DefaultWorkingHoursEnd { get; set; }
    public TimeOnly DefaultBreakTimeStart { get; set; }
    public TimeOnly DefaultBreakTimeEnd { get; set;}
    public int DefaultBookingDuration { get; set; }
    public RoleName DefaultUserRole { get; set; }
    public int Version { get; set; }
}