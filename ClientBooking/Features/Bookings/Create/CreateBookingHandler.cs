using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Shared.Enums;
using ClientBooking.Shared.Extensions;
using ClientBooking.Shared.Mapping;
using ClientBooking.Shared.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Bookings.Create;

public class CreateBookingHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/Booking/create/get/{clientId:int}", GetHandler).RequireAuthorization();
        app.MapPost("/booking/create", PostHandler).RequireAuthorization();
    }

    private static async Task<RazorComponentResult<CreateBookingComponent>> GetHandler(
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
                return new RazorComponentResult<CreateBookingComponent>(new 
                { 
                    ErrorMessage = "Client and/or user not found." 
                });
            }
            
            //Arrange default form fields based on the requested client and current user
            var bookingFormData = GetFormData(client, user, systemSettings);
            var createBookingRequest = new BookingRequest
            {
                StartDateTime = DateTime.Today.AddDays(1).Add(bookingFormData.WorkingHoursStart),
                EndDateTime = DateTime.Today.AddDays(1).Add(bookingFormData.WorkingHoursStart.Add(TimeSpan.FromHours(systemSettings.DefaultBookingDuration)))
            };

            return new RazorComponentResult<CreateBookingComponent>(new 
            { 
                CreateBookingRequest = createBookingRequest,
                BookingFormData = bookingFormData
            });
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<CreateBookingComponent>(new
            {
                ErrorMessage = $"An error occurred while loading the booking form: {ex.Message}"
            });
        }
    }

    private static async Task<Results<HtmxRedirectResult, RazorComponentResult<CreateBookingComponent>>> PostHandler(
        [FromForm] BookingRequest bookingRequest,
        [FromQuery] int clientId,
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
                return new RazorComponentResult<CreateBookingComponent>(new 
                { 
                    ErrorMessage = "Client and/or user not found." 
                });
            }
            
            //Quick simple validation check first.
            var validationResult = await validator.ValidateAsync(bookingRequest);
            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<CreateBookingComponent>(new
                {
                    CreateBookingRequest = bookingRequest,
                    BookingFormData = GetFormData(client, user, systemSettings),
                    ValidationErrors = validationResult.ToDictionary()
                });
            }

            //Perform complex validation, such as ensuring the request does not overlap user schedule and existing bookings
            //Optionally returns many BookingRequests, if a recurring request is detected.
            var analysedBookingRequestResult = await bookingService.EnforceBookingSchedulingRules(bookingRequest, client, user, systemSettings);
            if (analysedBookingRequestResult is { IsSuccess: false, ValidationErrors.Count: > 0 })
            {
                return new RazorComponentResult<CreateBookingComponent>(new
                {
                    CreateBookingRequest = bookingRequest,
                    BookingFormData = GetFormData(client, user, systemSettings),
                    analysedBookingRequestResult.ValidationErrors
                });
            }

            //If all new requests for booking are valid and have no conflicts, create the new bookings.
            var newBookings = analysedBookingRequestResult.Value.ToNewBookings(client, user);

            await dataContext.UserBookings.AddRangeAsync(newBookings);
            await dataContext.SaveChangesAsync();

            return new HtmxRedirectResult("/bookings");
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<CreateBookingComponent>(new
            {
                ErrorMessage = $"An unexpected error occurred while trying to create your bookings. Please try again later. {ex.Message}"
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
    
    private static RazorComponentResult<CreateBookingComponent> ReturnFormWithErrors(
        BookingRequest request, BookingFormData formData, string errorMessage)
    {
        return new RazorComponentResult<CreateBookingComponent>(new
        {
            CreateBookingRequest = request,
            BookingFormData = formData,
            ErrorMessage = errorMessage
        });
    }

    private static BookingFormData GetFormData(Client client, User user, Settings systemSettings)
    {
        var (workingHoursStart, workingHoursEnd, breakTimeStart, breakTimeEnd) = user.GetEffectiveWorkingHours(systemSettings);
        
        return new BookingFormData
        {
            Client = client,
            WorkingHoursStart = workingHoursStart,
            WorkingHoursEnd = workingHoursEnd,
            BreakTimeStart = breakTimeStart,
            BreakTimeEnd = breakTimeEnd,
            DoesWorkWeekends = user.DoesWorkWeekends
        };
    }
}