using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Shared.Mapping;
using ClientBooking.Shared.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Me.UpdateUser;

public class UpdateUserHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/user/get", GetHandler).RequireAuthorization();
        app.MapPost("/user/profile", PostHandler).RequireAuthorization();
        app.MapPost("/user/profile/toggle-working-hours", ToggleWorkingHours).RequireAuthorization();
        app.MapPost("/user/profile/toggle-break-time", ToggleBreakTime).RequireAuthorization();
    }

    private static async Task<Results<RazorComponentResult<UpdateUserComponent>, BadRequest<string>>>
        GetHandler(ISessionStateManager sessionStateManager, DataContext dataContext)
    {
        var userId = sessionStateManager.GetUserSessionId();

        if (userId == null)
        {
            return TypedResults.BadRequest("Unable to access user context.");
        }
        
        var user = await dataContext.Users.FindAsync(userId);

        if (user == null)
        {
            return TypedResults.BadRequest("Unable to load user context.");
        }
        
        var systemSettings = await dataContext.Settings
            .OrderByDescending(s => s.Version)
            .FirstAsync();
        
        var userProfile = user.MapToUserProfile(systemSettings);

        return new RazorComponentResult<UpdateUserComponent>(new { userProfile });
    }

    private static async Task<RazorComponentResult<UpdateUserComponent>>
        PostHandler([FromForm] UserProfile userProfile, ISessionStateManager sessionStateManager,
            IValidator<UserProfile> validator, DataContext dataContext, IUserWorkingHoursService userWorkingHoursService)
    {
        try
        {
            var userId = sessionStateManager.GetUserSessionId();
            
            if (userId is null)
            {
                return new RazorComponentResult<UpdateUserComponent>(new 
                { 
                    userProfile,
                    ErrorMessage = "User not found." 
                });
            }

            var user = await dataContext.Users.FindAsync(userId);
            
            if (user is null)
            {
                return new RazorComponentResult<UpdateUserComponent>(new 
                { 
                    userProfile,
                    ErrorMessage = "User not found." 
                });
            }
            
            var allValidationErrors = new Dictionary<string, string[]>();
            var validationResult = await validator.ValidateAsync(userProfile);
            
            if (!validationResult.IsValid)
            {
                foreach (var kvp in validationResult.ToDictionary())
                {
                    allValidationErrors.TryAdd(kvp.Key, kvp.Value);
                }
            }

            var userWorkingHoursValidation = await userWorkingHoursService.EnforceUserWorkingHoursRules(userProfile);

            if (userWorkingHoursValidation is { IsSuccess: false, ValidationErrors.Count: > 0 })
            {
                foreach (var kvp in userWorkingHoursValidation.ValidationErrors)
                {
                    allValidationErrors.TryAdd(kvp.Key, kvp.Value);
                }
            }
            
            if (allValidationErrors.Count > 0)
            {
                return new RazorComponentResult<UpdateUserComponent>(new
                {
                    userProfile,
                    ValidationErrors = allValidationErrors
                });
            }
            
            user.MapUserFromUpdateUserProfileRequest(userProfile);
            await dataContext.SaveChangesAsync();

            return new RazorComponentResult<UpdateUserComponent>(new
            {
                userProfile,
                ShowSuccessMessage = true
            });
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<UpdateUserComponent>(new
            {
                userProfile,
                ErrorMessage = ex.Message,
            });
        }
    }
    
    //TODO: Refactor Handler logic for WorkingHours/BreakHours to pull shared behaviour from a single method.
    private static async Task<RazorComponentResult<UpdateUserComponent>> ToggleWorkingHours(
        [FromForm] UserProfile userProfile,
        DataContext dataContext,
        ISessionStateManager sessionManager)
    {
        try
        {
            var userId = sessionManager.GetUserSessionId();
            var user = await dataContext.Users.FindAsync(userId);
        
            if (user != null)
            {
                user.UseSystemWorkingHours = userProfile.UseSystemWorkingHours;
                await dataContext.SaveChangesAsync();
            }
        
            var systemSettings = await dataContext.Settings
                .OrderByDescending(s => s.Version)
                .FirstAsync();

            var updatedProfile = user?.MapToUserProfile(systemSettings) ?? userProfile;

            return new RazorComponentResult<UpdateUserComponent>(new 
            { 
                UserProfile = updatedProfile,
                Section = "working-hours"
            });
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<UpdateUserComponent>(new 
            { 
                UserProfile = userProfile,
                Section = "working-hours",
                ErrorMessage = $"Failed to update working hours: {ex.Message} "
            });
        }
    }

    private static async Task<RazorComponentResult<UpdateUserComponent>> ToggleBreakTime(
        [FromForm] UserProfile userProfile,
        [FromServices] DataContext dataContext,
        [FromServices] ISessionStateManager sessionManager)
    {
        var userId = sessionManager.GetUserSessionId();
        var user = await dataContext.Users.FindAsync(userId);
        
        if (user != null)
        {
            user.UseSystemBreakTime = userProfile.UseSystemBreakTime;
            await dataContext.SaveChangesAsync();
        }
        
        var systemSettings = await dataContext.Settings
            .OrderByDescending(s => s.Version)
            .FirstAsync();

        return new RazorComponentResult<UpdateUserComponent>(new 
        { 
            UserProfile = user?.MapToUserProfile(systemSettings) ?? userProfile,
            Section = "break-time"
        });
    }
}

