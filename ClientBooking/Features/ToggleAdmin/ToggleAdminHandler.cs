using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.ToggleAdmin;

public class ToggleAdminHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/toggle/admin", Handler).RequireAuthorization();
    }

    //Request handler that toggles the administrator role for the current user.
    //The request is validated and used to assign or remove the administrator role.
    //This is used purely for testing purposes as this application is in a proof of concept stage.
    public static async Task<Results<HtmxRedirectResult, BadRequest<string>>> 
        Handler([FromForm] Request request, ISessionStateManager sessionStateManager, DataContext dataContext, ILogger<ToggleAdminHandler> logger)
    {
        try
        {
            //Access current user session.
            var userId = sessionStateManager.GetUserSessionId();

            if (userId == null)
            {
                logger.LogError("User Session not found when trying to toggle admin role.");
                return TypedResults.BadRequest("Error occurred accessing user session context");
            }
            
            //Access DB admin role, or create it if it does not exist.
            var adminRole = await dataContext.Roles.FirstOrDefaultAsync(x => x.Name == RoleName.Admin)
                            ?? await CreateAdminRoleIfNotExists(dataContext, logger);

            //Assign / remove administrator role from the current user session.
            switch (request.AssignAdministrator) 
            {
                case true:
                {
                    var newAdminUserRole = new UserRole{UserId = userId.Value,  RoleId = adminRole.Id};
                    await dataContext.UserRoles.AddAsync(newAdminUserRole);
                    
                    logger.LogInformation("User {userId} assigned administrator role.", userId);
                    break;
                }
                case false:
                {
                    var userRoleToDelete = await dataContext.UserRoles
                        .FirstOrDefaultAsync(x => x.UserId == userId.Value && x.RoleId == adminRole.Id);

                    if (userRoleToDelete is not null)
                    {
                        dataContext.Remove(userRoleToDelete);
                    }
                        
                    logger.LogInformation("User {userId} removed administrator role.", userId);
                    
                    break;
                }
            }

            //Commit to database, and ensure user session picks up new roles.
            await dataContext.SaveChangesAsync();
            
            await sessionStateManager.RefreshUserSession(dataContext);
            
            return new HtmxRedirectResult("/");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while trying to toggle admin role.");
            return TypedResults.BadRequest(ex.Message);
        }
    }

    public record Request(bool AssignAdministrator);

    //Helper method to create the administrator role if it does not exist.
    public static async Task<Role> CreateAdminRoleIfNotExists(DataContext dataContext, ILogger logger)
    {
        var adminRole = new Role { Name = RoleName.Admin };
        await dataContext.Roles.AddAsync(adminRole);
        await dataContext.SaveChangesAsync();
        
        logger.LogInformation("Created administrator role.");
        
        return adminRole;
    }
}