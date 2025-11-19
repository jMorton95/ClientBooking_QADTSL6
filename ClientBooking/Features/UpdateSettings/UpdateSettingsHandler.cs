using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Shared.Enums;
using ClientBooking.Shared.Mapping;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.UpdateSettings;

public class UpdateSettingsHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/settings/get", GetHandler).RequireAuthorization(nameof(RoleName.Admin));
        app.MapPost("/admin/settings", PostHandler).RequireAuthorization(nameof(RoleName.Admin));
    }

    private static async Task<RazorComponentResult<UpdateSettingsComponent>> GetHandler(DataContext dataContext)
    {
        var settings = await dataContext.Settings
            .OrderByDescending(x => x.Id)
            .FirstAsync();

        return new RazorComponentResult<UpdateSettingsComponent>(new {UpdateSettingsRequest = settings.ToUpdateSettingsRequest()});
    }
    
    private static async Task<RazorComponentResult<UpdateSettingsComponent>>
        PostHandler([FromForm] Request request, IValidator<UpdateSettingsRequest> validator, DataContext dataContext)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(request.UpdateSettingsRequest);

            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<UpdateSettingsComponent>(new
                {
                    request.UpdateSettingsRequest,
                    ValidationErrors = validationResult.ToDictionary()
                });
            }

            var newSettingsRecord = request.UpdateSettingsRequest.ToSettingsEntity();
            
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
            return new RazorComponentResult<UpdateSettingsComponent>(new
            {
                request.UpdateSettingsRequest,
                ErrorMessage = ex.Message
            });
        }
    }
    
    private record Request(UpdateSettingsRequest UpdateSettingsRequest);
}