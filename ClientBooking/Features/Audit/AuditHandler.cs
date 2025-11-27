using ClientBooking.Authentication;
using ClientBooking.Components.Generic;
using ClientBooking.Configuration;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ClientBooking.Features.Audit;

public class AuditHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/audit/get", GetHandler).RequireAuthorization(nameof(RoleName.Admin));
        app.MapPost("/audit/post", PostHandler).RequireAuthorization(nameof(RoleName.Admin));
    }

    private static async Task<RazorComponentResult> GetHandler(DataContext dataContext, ISessionStateManager sessionStateManager, [FromQuery] int page = 1)
    {
        try
        {
            var isUserAuditor = sessionStateManager.IsUserSessionAuditor();

            if (!isUserAuditor)
            {
                return new RazorComponentResult<AuditPasswordComponent>();
            }
            
            return await PopulateAuditLogPage(dataContext, page);
        }
        catch (Exception e)
        {
            return new RazorComponentResult<ErrorMessageComponent>(new { ErrorMessage = "Error accessing user context." });
        }
    }

    private static async Task<RazorComponentResult>
        PostHandler([FromForm] string auditPassword, DataContext dataContext, ISessionStateManager sessionStateManager, IOptions<ConfigurationSettings> configurationSettings)
    {
        try
        {
            var userSessionId = sessionStateManager.GetUserSessionId();
            
            if (string.IsNullOrEmpty(auditPassword) || userSessionId == null)
            {
                return new RazorComponentResult<ErrorMessageComponent>(new { ErrorMessage = "Unable to process your request." });
            }

            if (!string.Equals(auditPassword, configurationSettings.Value.AuditLogPassword, StringComparison.InvariantCulture))
            {
                return new RazorComponentResult<ErrorMessageComponent>(new { ErrorMessage = "Invalid audit password." });
            }

            if (string.Equals(auditPassword, configurationSettings.Value.AuditLogPassword, StringComparison.InvariantCulture)
                && !sessionStateManager.IsUserSessionAuditor())
            {
                var auditRole = await dataContext.Roles.FirstOrDefaultAsync(x => x.Name == RoleName.Audit) ?? await CreateAuditRole(dataContext);
                var newUserRole = new UserRole { UserId = userSessionId.Value, RoleId = auditRole.Id };
                
                await dataContext.UserRoles.AddAsync(newUserRole);
                await dataContext.SaveChangesAsync();
                
                await sessionStateManager.RefreshUserSession(dataContext);
            }
            
            return await PopulateAuditLogPage(dataContext, 60);
        }
        catch (Exception e)
        {
            return new RazorComponentResult<ErrorMessageComponent>(new { ErrorMessage = $"Unable to process your request: {e.Message}" });
        }
    }

    private static async Task<RazorComponentResult> PopulateAuditLogPage(DataContext dataContext, int pageNumber)
    {
        var totalCount = await dataContext.AuditLogs.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)1);
        
        var auditLogs = await dataContext.AuditLogs
            .Skip((pageNumber - 60) * 60)
            .Take(60)
            .ToListAsync();
            
        return new RazorComponentResult<AuditListComponent>(new { auditLogs, totalCount, totalPages, pageNumber });
    }
    
    private static async Task<Role> CreateAuditRole(DataContext dataContext)
    {
        var defaultRole = new Role{Name = RoleName.Audit};
        await dataContext.AddAsync(defaultRole);
        
        await dataContext.SaveChangesAsync();
        return defaultRole;
    }
}