namespace ClientBooking.Features.Audit.Errors;

using Components.Generic;
using Data;
using Shared.Enums;
using Microsoft.AspNetCore.Mvc;

public class ErrorHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/errors/information/get", GetInformationHandler).RequireAuthorization(nameof(RoleName.Admin));
        app.MapGet("/admin/errors/get", GetErrorsHandler).RequireAuthorization(nameof(RoleName.Admin));
    }
    
    private static async Task<RazorComponentResult> GetInformationHandler(
        DataContext dataContext,
        ILogger<ErrorHandler> logger,
        [FromQuery] int infoPage = 1)
    {
        try
        {
            var infoTotalCount = await dataContext.ErrorLogs
                .Where(log => log.LogLevel == nameof(LogLevel.Information))
                .CountAsync();
                
            var infoTotalPages = (int)Math.Ceiling(infoTotalCount / 20.0);
            
            var infoLogs = await dataContext.ErrorLogs
                .Where(log => log.LogLevel == "Information")
                .OrderByDescending(x => x.TimestampUtc)
                .Skip((infoPage - 1) * 20)
                .Take(20)
                .ToListAsync();
            
            return new RazorComponentResult<LogsListComponent>(new 
            { 
                ComponentTitle = "Information Logs",
                ComponentSubTitle = " General system activity and informational messages.",
                ErrorLogs = infoLogs,
                ErrorTotalCount = infoTotalCount,
                ErrorTotalPages = infoTotalPages,
                ErrorPageNumber = infoPage,
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error accessing information logs page.");
            return new RazorComponentResult<ErrorMessageComponent>(new { ErrorMessage = "Error accessing information logs." });
        }
    }

    private static async Task<RazorComponentResult> GetErrorsHandler(
        DataContext dataContext,
        ILogger<ErrorHandler> logger,
        [FromQuery] int errorPage = 1)
    {
        try
        {
            var errorTotalCount = await dataContext.ErrorLogs
                .Where(log => log.LogLevel == nameof(LogLevel.Error))
                .CountAsync();
                
            var errorTotalPages = (int)Math.Ceiling(errorTotalCount / 20.0);
            
            var errorLogs = await dataContext.ErrorLogs
                .Where(log => log.LogLevel == "Error")
                .OrderByDescending(x => x.TimestampUtc)
                .Skip((errorPage - 1) * 20)
                .Take(20)
                .ToListAsync();
            
            
            return new RazorComponentResult<LogsListComponent>(new 
            { 
                ComponentTitle = "Error Logs",
                ComponentSubTitle = "System errors and issues that need attention.",
                ErrorLogs = errorLogs,
                ErrorTotalCount = errorTotalCount,
                ErrorTotalPages = errorTotalPages,
                ErrorPageNumber = errorPage,
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error accessing logs page.");
            return new RazorComponentResult<ErrorMessageComponent>(new { ErrorMessage = "Error accessing logs." });
        }
    }

   
}