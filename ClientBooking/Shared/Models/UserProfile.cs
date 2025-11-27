using ClientBooking.Data.Entities;

namespace ClientBooking.Shared.Models;

public class UserProfile
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public TimeOnly WorkingHoursStart { get; set; }
    public TimeOnly WorkingHoursEnd { get; set; }
    public TimeOnly BreakTimeStart { get; set; }
    public TimeOnly BreakTimeEnd { get; set; }
    public bool DoesWorkWeekends { get; set; }
    
    public bool UseSystemWorkingHours { get; set; } = true;
    public bool UseSystemBreakTime { get; set; } = true;
    
    public void AssignEffectiveHours(Settings systemSettings)
    {
        WorkingHoursStart = UseSystemWorkingHours ? systemSettings.DefaultWorkingHoursStart.ToTimeOnly() : WorkingHoursStart;
        WorkingHoursEnd = UseSystemWorkingHours ? systemSettings.DefaultWorkingHoursEnd.ToTimeOnly() : WorkingHoursEnd;
        BreakTimeStart = UseSystemBreakTime ? systemSettings.DefaultBreakTimeStart.ToTimeOnly() : BreakTimeStart;
        BreakTimeEnd = UseSystemBreakTime ? systemSettings.DefaultBreakTimeEnd.ToTimeOnly() : BreakTimeEnd;
    }
}