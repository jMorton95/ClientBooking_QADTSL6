using ClientBooking.Authentication;
using ClientBooking.Components.Generic;
using ClientBooking.Data;
using ClientBooking.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Audit;

public class AuditHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/audit/get", GetHandler).RequireAuthorization(nameof(RoleName.Admin));
    }

    //Request handler that returns the audit log page for administrators.
    public static async Task<RazorComponentResult> GetHandler(
        DataContext dataContext,
        ISessionStateManager sessionStateManager,
        ILogger<AuditHandler> logger,
        [FromQuery] int page = 1)
    {
        try
        {
            return await PopulateAuditLogPage(dataContext, page);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error accessing audit log page.");
            return new RazorComponentResult<ErrorMessageComponent>(new { ErrorMessage = "Error accessing user context." });
        }
    }
    
    //Helper method that populates the audit log page with the audit logs.
    private static async Task<RazorComponentResult> PopulateAuditLogPage(DataContext dataContext, int pageNumber)
    {
        var totalCount = await dataContext.AuditLogs.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)60);
        
        var auditLogs = await dataContext.AuditLogs
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * 60)
            .Take(60)
            .ToListAsync();
            
        return new RazorComponentResult<AuditListComponent>(new { auditLogs, totalCount, totalPages, pageNumber });
    }
}