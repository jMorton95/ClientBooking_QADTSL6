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

    private static async Task<Results<HtmxRedirectResult, BadRequest<string>>> 
        Handler([FromForm] Request request, ISessionStateManager sessionStateManager, DataContext dataContext)
    {
        try
        {
            //Access current user session.
            var userId = sessionStateManager.GetUserSessionId();

            if (userId == null)
            {
                return TypedResults.BadRequest("Error occurred accessing user session context");
            }
            
            
            //Access DB admin role, or create it if it does not exist.
            var adminRole = await dataContext.Roles.FirstOrDefaultAsync(x => x.Name == RoleName.Admin)
                            ?? await CreateAdminRoleIfNotExists(dataContext);

            //Assign / remove administrator role from the current user session.
            switch (request.AssignAdministrator) 
            {
                case true:
                {
                    var newAdminUserRole = new UserRole{UserId = userId.Value,  RoleId = adminRole.Id};
                    await dataContext.UserRoles.AddAsync(newAdminUserRole);
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

                    break;
                }
            }

            //Commit to database, and ensure user session picks up new roles.
            await dataContext.SaveChangesAsync();
            
            await RefreshUserSession(dataContext, sessionStateManager, userId.Value);
            
            return new HtmxRedirectResult("/");
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private record Request(bool AssignAdministrator);

    private static async Task<Role> CreateAdminRoleIfNotExists(DataContext dataContext)
    {
        var adminRole = new Role { Name = RoleName.Admin };
        await dataContext.Roles.AddAsync(adminRole);
        await dataContext.SaveChangesAsync();
        return adminRole;
    }

    private static async Task RefreshUserSession(DataContext dataContext, ISessionStateManager sessionStateManager, int userId)
    {
        var user = await dataContext.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            return;
        }
        
        await sessionStateManager.LoginAsync(user);
    }
}