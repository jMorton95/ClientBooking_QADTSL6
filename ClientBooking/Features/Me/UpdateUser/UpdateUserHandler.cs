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
        app.MapPost("/user/", PostHandler).RequireAuthorization();
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
        
        var userProfile = user.MapToUserProfile();

        return new RazorComponentResult<UpdateUserComponent>(new { userProfile });

    }

    private static async Task<RazorComponentResult<UpdateUserComponent>>
        PostHandler([FromForm] UserProfile userProfile, ISessionStateManager sessionStateManager, IValidator<UserProfile> validator, DataContext dataContext)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(userProfile);
            
            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<UpdateUserComponent>(new
                {
                    userProfile,
                    ValidationErrors = validationResult.ToDictionary()
                });
            }
            
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
}

