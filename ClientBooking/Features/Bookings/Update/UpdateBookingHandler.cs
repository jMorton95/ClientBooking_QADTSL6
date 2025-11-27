using ClientBooking.Authentication;
using ClientBooking.Data;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Bookings.Update;

public class UpdateBookingHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/booking/update/get/{bookingId:int}", GetHandler).RequireAuthorization();
        app.MapPost("/booking/update/{bookingId:int}", PostHandler).RequireAuthorization();
        app.MapPost("/booking/update/{bookingId:int}/toggle-recurring", ToggleRecurringSection).RequireAuthorization();
    }

    private static async Task<RazorComponentResult<BookingFormComponent>> GetHandler(
        [FromRoute] int bookingId,
        [FromServices] DataContext dataContext,
        [FromServices] ISessionStateManager sessionManager)
    {
        try
        {
            var userId = sessionManager.GetUserSessionId();
            if (userId == null)
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "User not found." 
                });
            }

            var booking = await dataContext.Bookings
                .Include(b => b.Client)
                .Include(b => b.UserBookings)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "Booking not found." 
                });
            }

            if (booking.UserBookings.All(ub => ub.UserId != userId))
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "You don't have permission to edit this booking." 
                });
            }

            var user = await dataContext.Users.FindAsync(userId);
            var systemSettings = await dataContext.Settings
                .OrderByDescending(s => s.Version)
                .FirstAsync();

            if (user == null)
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "User not found." 
                });
            }

            var bookingRequest = new BookingRequest
            {
                Notes = booking.Notes,
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                IsRecurring = booking.IsRecurring,
                NumberOfRecurrences = booking.NumberOfRecurrences,
                RecurrencePattern = booking.RecurrencePattern
            };

            var bookingFormData = BookingFormData.GetFormData(booking.Client, user, systemSettings);

            return new RazorComponentResult<BookingFormComponent>(new 
            { 
                BookingRequest = bookingRequest,
                BookingFormData = bookingFormData,
                BookingId = bookingId,
                IsEditMode = true
            });
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<BookingFormComponent>(new
            {
                ErrorMessage = $"An error occurred while loading the booking: {ex.Message}"
            });
        }
    }

    private static async Task<Results<HtmxRedirectResult, RazorComponentResult<BookingFormComponent>>> PostHandler(
        [FromForm] BookingRequest bookingRequest,
        [FromRoute] int bookingId,
        IValidator<BookingRequest> validator, DataContext dataContext, ISessionStateManager sessionManager, IBookingService bookingService)
    {
        try
        {
            var userId = sessionManager.GetUserSessionId();
            var existingBooking = await dataContext.Bookings
                .Include(b => b.Client)
                .Include(b => b.UserBookings)
                    .ThenInclude(userBooking => userBooking.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (userId == null || existingBooking == null)
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "Booking not found." 
                });
            }

            if (existingBooking.UserBookings.All(ub => ub.UserId != userId))
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "You don't have permission to edit this booking." 
                });
            }
            
            var user = await dataContext.Users.FindAsync(userId);
            var systemSettings = await dataContext.Settings.OrderByDescending(s => s.Version).FirstAsync();

            var validationResult = await validator.ValidateAsync(bookingRequest);
            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<BookingFormComponent>(new
                {
                    BookingRequest = bookingRequest,
                    BookingFormData = BookingFormData.GetFormData(existingBooking.Client, user!, systemSettings),
                    BookingId = bookingId,
                    IsEditMode = true,
                    ValidationErrors = validationResult.ToDictionary()
                });
            }

            var seriesIdToExclude = existingBooking?.RecurrenceSeriesId;
            
            var analysedBookingRequestResult = await bookingService.EnforceBookingSchedulingRules(bookingRequest, 
                    client: existingBooking!.Client, user: existingBooking.UserBookings.First().User, 
                    settings: systemSettings,
                    optionalExcludeBookingId: bookingId,
                    optionalExcludeSeriesId: seriesIdToExclude);

            if (analysedBookingRequestResult is { IsSuccess: false, ValidationErrors.Count: > 0 })
            {
                return new RazorComponentResult<BookingFormComponent>(new
                {
                    BookingRequest = bookingRequest,
                    BookingFormData = BookingFormData.GetFormData(existingBooking.Client, user!, systemSettings),
                    BookingId = bookingId,
                    IsEditMode = true,
                    analysedBookingRequestResult.ValidationErrors
                });
            }
            
            await bookingService.UpdateBooking(existingBooking, analysedBookingRequestResult.Value, existingBooking.Client, userId.Value);
            
            return new HtmxRedirectResult("/bookings");
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<BookingFormComponent>(new
            {
                ErrorMessage = $"An unexpected error occurred while updating the booking: {ex.Message}"
            });
        }
    }

    private static async Task<RazorComponentResult<BookingFormComponent>> ToggleRecurringSection(
        [FromForm] BookingRequest bookingRequest,
        [FromRoute] int bookingId,
        [FromServices] DataContext dataContext,
        [FromServices] ISessionStateManager sessionManager)
    {
        try
        {
            var userId = sessionManager.GetUserSessionId();
            if (userId == null)
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "User not found." 
                });
            }

            var booking = await dataContext.Bookings
                .Include(b => b.Client)
                .Include(b => b.UserBookings)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.UserBookings.All(ub => ub.UserId != userId))
            {
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "Booking not found or access denied." 
                });
            }

            var user = await dataContext.Users.FindAsync(userId);
            var systemSettings = await dataContext.Settings.OrderByDescending(s => s.Version).FirstAsync();

            return new RazorComponentResult<BookingFormComponent>(new 
            { 
                BookingRequest = bookingRequest,
                BookingFormData = BookingFormData.GetFormData(booking.Client, user!, systemSettings),
                BookingId = bookingId,
                IsEditMode = true,
                Section = "recurring"
            });
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<BookingFormComponent>(new 
            { 
                BookingRequest = bookingRequest,
                BookingId = bookingId,
                IsEditMode = true,
                Section = "recurring",
                ErrorMessage = $"Failed to toggle recurring section: {ex.Message}"
            });
        }
    }
}