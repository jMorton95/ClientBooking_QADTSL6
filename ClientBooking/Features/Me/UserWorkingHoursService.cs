using ClientBooking.Data;
using ClientBooking.Shared.Models;

namespace ClientBooking.Features.Me;

public interface IUserWorkingHoursService
{
    public Task<Result<UserProfile>> EnforceUserWorkingHoursRules(UserProfile userProfile);
}

//Service that validates user working hours against system settings
public class UserWorkingHoursService(DataContext dataContext) : IUserWorkingHoursService
{
    public async Task<Result<UserProfile>> EnforceUserWorkingHoursRules(UserProfile userProfile)
    {
        //Grab effective working hours for validation purposes, as these properties can come from a mixture of the request and system default settings
        if (userProfile.UseSystemBreakTime || userProfile.UseSystemWorkingHours)
        {
            var systemSettings = await dataContext.Settings.OrderByDescending(x => x.Version).FirstAsync();
            userProfile.AssignEffectiveHours(systemSettings);
        }
        
        var validationErrors = ValidateEffectiveHours(userProfile);
        
        return validationErrors.Count != 0
            ? Result<UserProfile>.ValidationFailure(validationErrors)
            : Result<UserProfile>.Success(userProfile);
    }
    
    public Dictionary<string, string[]> ValidateEffectiveHours(UserProfile userProfile)
    {
        var errors = new Dictionary<string, string[]>();

        //Validate break time is within working hours
        if (userProfile.BreakTimeStart < userProfile.WorkingHoursStart || 
            userProfile.BreakTimeEnd > userProfile.WorkingHoursEnd)
        {
            errors["BreakTimeRange"] = 
                [$"Your break time (currently {userProfile.BreakTimeStart} to {userProfile.BreakTimeEnd}) must be within your working hours range of {userProfile.WorkingHoursStart} and {userProfile.WorkingHoursEnd} hours."];
        }

        //Ensure users schedules are at minimum 7 hours.
        var workingDuration = userProfile.WorkingHoursEnd - userProfile.WorkingHoursStart;
        var breakDuration = userProfile.BreakTimeEnd - userProfile.BreakTimeStart;
        var netWorkingHours = workingDuration - breakDuration;

        if (netWorkingHours < TimeSpan.FromHours(7))
        {
            errors["TotalWorkingHours"] = [$"Total shift time must be at least 7 hours (excluding breaks). With your proposal, you will only work {workingDuration.TotalHours} hours."];
        }

        return errors;
    }
}