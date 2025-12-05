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

    //Request handler that returns the booking form page.
    //The booking id is used to retrieve the booking entity from the database.
    //The booking request is used to pre-populate the form fields.
    //The booking id is also used to determine whether the user has permission to edit the booking.
    public static async Task<RazorComponentResult<BookingFormComponent>> GetHandler(
        [FromRoute] int bookingId,
        [FromServices] DataContext dataContext,
        [FromServices] ISessionStateManager sessionManager,
        ILogger<UpdateBookingHandler> logger)
    {
        try
        {
            var userId = sessionManager.GetUserSessionId();
            if (userId == null)
            {
                logger.LogError("User Session not found when trying to load booking: {bookingId}.", bookingId);
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
                logger.LogError("Booking not found when trying to load booking: {bookingId}.", bookingId);
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "Booking not found." 
                });
            }

            if (booking.UserBookings.All(ub => ub.UserId != userId))
            {
                logger.LogError("User {userId} does not have permission to edit booking {bookingId}.", userId, bookingId);
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
                logger.LogError("User not found when trying to load booking: {bookingId}.", bookingId);
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
            logger.LogError(ex, "An error occurred while loading the booking: {bookingId}.", bookingId);
            return new RazorComponentResult<BookingFormComponent>(new
            {
                ErrorMessage = $"An error occurred while loading the booking: {ex.Message}"
            });
        }
    }

    //Request handler that updates an existing booking based on the booking request.
    //The booking id is used to retrieve the booking entity from the database.
    //The booking request is validated and used to update the booking.
    //The booking id is also used to determine whether the user has permission to edit the booking.
    //The booking service is used to enforce booking scheduling rules.
    public static async Task<Results<HtmxRedirectResult, RazorComponentResult<BookingFormComponent>>> PostHandler(
        [FromForm] BookingRequest bookingRequest,
        [FromRoute] int bookingId,
        IValidator<BookingRequest> validator,
        DataContext dataContext,
        ISessionStateManager sessionManager,
        IBookingService bookingService,
        ILogger<UpdateBookingHandler> logger)
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
                logger.LogError("User Session or Booking not found when trying to update booking: {bookingId}.", bookingId);
                return new RazorComponentResult<BookingFormComponent>(new 
                { 
                    ErrorMessage = "Booking not found." 
                });
            }

            if (existingBooking.UserBookings.All(ub => ub.UserId != userId))
            {
                logger.LogError("User {userId} does not have permission to update booking {bookingId}.", userId, bookingId);
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
                logger.LogError("Validation failed for booking request by user {UserId} for booking {BookingId}.", userId, bookingId);
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
                logger.LogError("Enforcement rules failed for booking request by user {UserId} for booking {BookingId}.", userId, bookingId);
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
            logger.LogError(ex, "An unexpected error occurred while trying to update booking: {bookingId}.", bookingId);
            return new RazorComponentResult<BookingFormComponent>(new
            {
                ErrorMessage = $"An unexpected error occurred while updating the booking: {ex.Message}"
            });
        }
    }

    
    //Request handler that toggles the recurring section of the booking form.
    //The booking id is used to retrieve the booking entity from the database.
    //The booking request is used to pre-populate the form fields.
    public static async Task<RazorComponentResult<BookingFormComponent>> ToggleRecurringSection(
        [FromForm] BookingRequest bookingRequest,
        [FromRoute] int bookingId,
        [FromServices] DataContext dataContext,
        [FromServices] ISessionStateManager sessionManager,
        ILogger<UpdateBookingHandler> logger)
    {
        try
        {
            var userId = sessionManager.GetUserSessionId();
            if (userId == null)
            {
                logger.LogError("User Session not found when trying to toggle recurring section.");
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
                logger.LogError("User {userId} does not have permission to toggle recurring section for booking {bookingId}.", userId, bookingId);
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
            logger.LogError(ex, "An error occurred while trying to toggle recurring section for booking: {bookingId}.", bookingId);
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