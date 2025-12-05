using ClientBooking.Data;
using ClientBooking.Shared.Enums;
using ClientBooking.Shared.Mapping;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.UpdateSettings;

public class UpdateSettingsHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        //Endpoints require admin role to access
        app.MapGet("/admin/settings/get", GetHandler).RequireAuthorization(nameof(RoleName.Admin));
        app.MapPost("/admin/settings", PostHandler).RequireAuthorization(nameof(RoleName.Admin));
    }

    //Request handler that returns the settings page.
    //The most recent settings record is retrieved from the database and used to pre-populate the form fields.
    public static async Task<RazorComponentResult<UpdateSettingsComponent>> GetHandler(DataContext dataContext)
    {
        var settings = await dataContext.Settings
            .OrderByDescending(x => x.Version)
            .FirstAsync();

        return new RazorComponentResult<UpdateSettingsComponent>(new {UpdateSettingsRequest = settings.ToUpdateSettingsRequest()});
    }
    
    //Request handler that updates the settings record.
    //The update settings request is validated and used to create a new settings record.
    public static async Task<RazorComponentResult<UpdateSettingsComponent>>
        PostHandler(
            [FromForm] UpdateSettingsRequest updateSettingsRequest,
            IValidator<UpdateSettingsRequest> validator,
            DataContext dataContext,
            ILogger<UpdateSettingsHandler> logger)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(updateSettingsRequest);

            if (!validationResult.IsValid)
            {
                logger.LogError("Validation failed for update settings request.");
                return new RazorComponentResult<UpdateSettingsComponent>(new
                {
                    updateSettingsRequest,
                    ValidationErrors = validationResult.ToDictionary()
                });
            }

            var newSettingsRecord = updateSettingsRequest.ToSettingsEntity();
            
            await dataContext.AddAsync(newSettingsRecord);
            await dataContext.SaveChangesAsync();

            return new RazorComponentResult<UpdateSettingsComponent>(new
            {
                UpdateSettingsRequest = newSettingsRecord.ToUpdateSettingsRequest(),
                ShowSuccessMessage = true,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred updating settings.");
            return new RazorComponentResult<UpdateSettingsComponent>(new
            {
                updateSettingsRequest,
                ErrorMessage = ex.Message
            });
        }
    }
}