using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Shared.Extensions;
using ClientBooking.Shared.Mapping;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Bookings.Create;

public class CreateBookingHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/booking/create/get/{clientId:int}", GetHandler).RequireAuthorization();
        app.MapPost("/booking/create/{clientId:int}", PostHandler).RequireAuthorization();
        app.MapPost("/booking/create/{clientId:int}/toggle-recurring", ToggleRecurringSection).RequireAuthorization();
    }

    private static async Task<RazorComponentResult<BookingFormComponent>> GetHandler(
        [FromRoute] int clientId,
        [FromServices] DataContext dataContext,
        [FromServices] ISessionStateManager sessionManager)
    {
        try
        {
            //Ensure all entities related in this transaction are present.
            var (userId, user, client, systemSettings) = await ArrangeRequestEntities(sessionManager, clientId, dataContext);
            if (userId is null || client is null || user is null)
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "Client and/or user not found." 
                });
            }
            
            //Arrange default form fields based on the requested client and current user
            var bookingFormData = BookingFormData.GetFormData(client, user, systemSettings);
            var bookingRequest = new BookingRequest
            {
                StartDateTime = DateTime.Today.AddDays(1).Add(bookingFormData.WorkingHoursStart),
                EndDateTime = DateTime.Today.AddDays(1).Add(bookingFormData.WorkingHoursStart.Add(TimeSpan.FromMinutes(30)))
            };

            return new RazorComponentResult<BookingFormComponent>(new 
            { 
                bookingRequest,
                BookingFormData = bookingFormData
            });
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<BookingFormComponent>(new
            {
                ErrorMessage = $"An error occurred while loading the booking form: {ex.Message}"
            });
        }
    }

    private static async Task<Results<HtmxRedirectResult, RazorComponentResult<BookingFormComponent>>> PostHandler(
        [FromForm] BookingRequest bookingRequest,
        [FromRoute] int clientId,
        [FromServices] IValidator<BookingRequest> validator,
        [FromServices] DataContext dataContext,
        [FromServices] ISessionStateManager sessionManager,
        [FromServices] IBookingService bookingService)
    {
        try
        {
            //Ensure all entities related in this transaction are present.
            var (userId, user, client, systemSettings) = await ArrangeRequestEntities(sessionManager, clientId, dataContext);
            if (userId is null || client is null || user is null)
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "Client and/or user not found." 
                });
            }
            
            //Quick simple validation check first.
            var validationResult = await validator.ValidateAsync(bookingRequest);
            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<BookingFormComponent>(new
                {
                    bookingRequest,
                    BookingFormData = BookingFormData.GetFormData(client, user, systemSettings),
                    ValidationErrors = validationResult.ToDictionary()
                });
            }

            //Perform complex validation, such as ensuring the request does not overlap user schedule and existing bookings
            //Optionally returns many BookingRequests, if a recurring request is detected.
            var analysedBookingRequestResult = await bookingService.EnforceBookingSchedulingRules(bookingRequest, client, user, systemSettings, null);
            if (analysedBookingRequestResult is { IsSuccess: false, ValidationErrors.Count: > 0 })
            {
                return new RazorComponentResult<BookingFormComponent>(new
                {
                    bookingRequest,
                    BookingFormData = BookingFormData.GetFormData(client, user, systemSettings),
                    analysedBookingRequestResult.ValidationErrors
                });
            }

            //If all new requests for booking are valid and have no conflicts, create the new bookings.
            await bookingService.CreateBooking(analysedBookingRequestResult.Value, client, user.Id);

            return new HtmxRedirectResult("/");
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<BookingFormComponent>(new
            {
                ErrorMessage = $"An unexpected error occurred while trying to create your bookings. Please try again later. {ex.Message}"
            });
        }
    }
    
    private static async Task<RazorComponentResult<BookingFormComponent>> ToggleRecurringSection(
        [FromForm] BookingRequest bookingRequest,
        [FromRoute] int clientId,
        DataContext dataContext,
        ISessionStateManager sessionManager)
    {
        try
        {
            var (userId, user, client, systemSettings) = await ArrangeRequestEntities(sessionManager, clientId, dataContext);
            if (userId is null || client is null || user is null)
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "Client and/or user not found." 
                });
            }

            return new RazorComponentResult<BookingFormComponent>(new 
            { 
                bookingRequest,
                BookingFormData = BookingFormData.GetFormData(client, user, systemSettings),
                Section = "recurring"
            });
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<BookingFormComponent>(new 
            { 
                bookingRequest,
                Section = "recurring",
                ErrorMessage = $"Failed to toggle recurring section: {ex.Message} "
            });
        }
    }


    private static async Task<(int? UserId, User? User, Client? Client, Settings SystemSettings)> 
        ArrangeRequestEntities(ISessionStateManager sessionStateManager, int clientId, DataContext dataContext)
    {
        var userId = sessionStateManager.GetUserSessionId();
        var client = await dataContext.Clients
            .Include(x => x.Bookings)
            .FirstOrDefaultAsync(c => c.Id == clientId);
            
        var user = await dataContext.Users.FindAsync(userId);
        
        var systemSettings = await dataContext.Settings
            .OrderByDescending(s => s.Version)
            .FirstAsync();

        return (userId, user, client, systemSettings);
    }
    
   
}